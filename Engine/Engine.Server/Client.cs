﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;
using FTW.Engine.Shared;

namespace FTW.Engine.Server
{
    public abstract class Client
    {
        protected Client()
        {
            NeedsFullUpdate = true;
            SnapshotInterval = 50; // this should be a Variable
        }

        public string Name
        {
            get { return GetVariable("name"); }
            set { SetVariable("name", GetUniqueName(value)); }
        }
        private RakNetGUID guid;
        internal RakNetGUID UniqueID { get { return guid; } set { guid = value; ID = guid.systemIndex; } }
        public ushort ID { get; private set; }
        internal uint LastSnapshotTime { get; set; }
        internal uint NextSnapshotTime { get; set; }
        internal uint SnapshotInterval { get; set; }
        internal bool NeedsFullUpdate { get; set; }
        internal bool FullyConnected { get; set; }
        private SortedList<ushort, bool> KnownEntities = new SortedList<ushort, bool>();
        internal SortedList<ushort, bool> DeletedEntities = new SortedList<ushort, bool>();

        public abstract bool IsLocal { get; }

        internal static SortedList<ulong, Client> AllClients = new SortedList<ulong, Client>();
        public IList<Client> GetAll() { return AllClients.Values; }

        public static List<Client> GetAllExcept(params Client[] exclude)
        {
            List<Client> list = new List<Client>();
            foreach (Client c in AllClients.Values)
            {
                bool excluded = false;
                foreach (Client ex in exclude)
                    if (c == ex)
                    {
                        excluded = true;
                        break;
                    }
                if (!excluded)
                    list.Add(c);
            }
            return list;
        }

        public static List<Client> GetAllExcept(IEnumerator<Client> exclude)
        {
            List<Client> list = new List<Client>();
            foreach (Client c in AllClients.Values)
            {
                exclude.Reset();
                bool excluded = false;
                do
                {
                    if (c == exclude.Current)
                    {
                        excluded = true;
                        break;
                    }
                } while (exclude.MoveNext());

                if (!excluded)
                    list.Add(c);
            }
            return list;
        }

        public static List<Client> GetAllExceptLocal()
        {
            List<Client> cl = new List<Client>();
            foreach (Client c in AllClients.Values)
            {
                if (c.IsLocal)
                    continue;
                cl.Add(c);
            }
            return cl;
        }

        public static Client LocalClient { get; internal set; }

        public static Client GetByName(string name)
        {
            foreach (Client c in AllClients.Values)
                if (c.Name == name)
                    return c;
            return null;
        }

        internal static Client GetByUniqueID(RakNetGUID uniqueID)
        {
            if (AllClients.ContainsKey(uniqueID.g))
                return AllClients[uniqueID.g];
            return null;
        }

        internal string GetUniqueName(string desiredName)
        {
            desiredName = GameServer.Instance.ValidatePlayerName(desiredName);

            string newName = desiredName;
            int i = 1;

            Client matchingName = Client.GetByName(newName);
            while (matchingName != null && matchingName != this)
            {
                newName = string.Format("{0} ({1})", desiredName, i);
                i++;

                matchingName = Client.GetByName(newName);
            }
            return newName;
        }

        public abstract void Send(Message m);

        public static void SendToAll(Message m)
        {
            GameServer.Instance.rakNet.Send(m.Stream, m.Priority, m.Reliability, (char)m.OrderingChannel, RakNet.RakNet.UNASSIGNED_RAKNET_GUID, true);
            if (LocalClient != null)
                LocalClient.Send(m);
        }

        public static void SendToAllExcept(Message m, Client c)
        {
            GameServer.Instance.rakNet.Send(m.Stream, m.Priority, m.Reliability, (char)0, c.UniqueID, true);
            if (c != LocalClient && LocalClient != null)
                LocalClient.Send(m);
        }

        internal static void SendSnapshots()
        {
            foreach (Client c in AllClients.Values)
            {
                if (c.NextSnapshotTime > GameServer.Instance.FrameTime)
                    continue;

                c.SendSnapshot();
                c.LastSnapshotTime = GameServer.Instance.FrameTime;
                c.NextSnapshotTime += c.SnapshotInterval;
            }
        }

        private void SendSnapshot()
        {
            Message m = new Message((byte)DefaultMessageIDTypes.ID_TIMESTAMP, PacketPriority.HIGH_PRIORITY, PacketReliability.UNRELIABLE, 0);
            m.Write(GameServer.Instance.FrameTime);
            m.Write((byte)EngineMessage.Snapshot);

            // now step through all entities, decide what to send. Will either be:
            // No change (sends nothing)
            // New entity / full update of entity (Full)
            // Partial update of entity (Partial)
            // Forget about this entity (Delete)
            // ID reassigned, full update of new entity (Replace)
            foreach (Entity e in Entity.AllEntities)
            {
                if (!e.IsNetworked)
                    continue;
                
                NetworkedEntity ne = e as NetworkedEntity;
                if (ne.IsDeleted || !ne.HasChanges(this))
                    continue;

                if (!ne.ShouldSendToClient(this))
                {
                    if (KnownEntities.ContainsKey(ne.EntityID))
                        DeletedEntities.Add(ne.EntityID, false); // the client shouldn't see this entity any more, so delete it on their end
                    continue;
                }

                m.Write(ne.EntityID);

                EntitySnapshotType updateType;
                if (KnownEntities.ContainsKey(ne.EntityID))
                {
                    if (DeletedEntities.ContainsKey(ne.EntityID))
                    {
                        // Reusing an entity ID the client is still using for something else. Ensure it knows the old should be deleted.
                        // Also, don't include it on the Delete list - the Replace accounts for that.
                        updateType = EntitySnapshotType.Replace;
                        DeletedEntities.Remove(ne.EntityID);
                    }
                    else
                        updateType = NeedsFullUpdate ? EntitySnapshotType.Full : EntitySnapshotType.Partial;
                }
                else
                {
                    updateType = EntitySnapshotType.Full;
                    KnownEntities[ne.EntityID] = true;
                }

                m.Write((byte)updateType);
                ne.WriteSnapshot(m, this, updateType == EntitySnapshotType.Partial);
            }

            foreach (ushort entityID in DeletedEntities.Keys)
            {
                m.Write(entityID);
                m.Write((byte)EntitySnapshotType.Delete);
                KnownEntities.Remove(entityID);
            }
            DeletedEntities.Clear();

            Send(m);
            NeedsFullUpdate = false;
        }

        private SortedList<string, string> variables = new SortedList<string, string>();
        private SortedList<string, float> numericVariables = new SortedList<string, float>();
        internal void SetVariable(string name, string val)
        {
            if (name == "name")
                val = GetUniqueName(val);
            else if (name == "cl_snapshotrate")
            {
                float f;
                if (float.TryParse(val, out f) && f != 0f)
                    SnapshotInterval = (uint)(1000f / f);
            }

            variables[name] = val;
            float num;
            if (float.TryParse(val, out num))
                numericVariables[name] = num;
            else
                numericVariables.Remove(name);
        }

        internal void SetVariable(string name, float val)
        {
            variables[name] = val.ToString();
            numericVariables[name] = val;
        }

        public string GetVariable(string name)
        {
            string val;
            if (variables.TryGetValue(name, out val))
                return val;
            return null;
        }

        public float? GetNumericVariable(string name)
        {
            float val;
            if (numericVariables.TryGetValue(name, out val))
                return val;
            return null;
        }
    }

    internal class LocalClient : Client
    {
        private LocalClient() { UniqueID = RakNet.RakNet.UNASSIGNED_RAKNET_GUID; }
        public override bool IsLocal { get { return true; } }

        public static Client Create(string desiredName)
        {
            LocalClient c = new LocalClient();
            c.Name = desiredName;

            AllClients.Add(RakNet.RakNet.UNASSIGNED_RAKNET_GUID.g, c);
            LocalClient = c;
            return c;
        }

        public override void Send(Message m)
        {
            m.ResetRead();

            lock (Message.ToLocalClient)
                Message.ToLocalClient.Add(m);
        }
    }

    internal class RemoteClient : Client
    {
        private RemoteClient(RakNetGUID id) { UniqueID = id; }
        public override bool IsLocal { get { return false; } }

        public static Client Create(RakNetGUID uniqueID)
        {
            RemoteClient c = new RemoteClient(uniqueID);
            c.Name = "unknown";

            AllClients.Add(uniqueID.g, c);
            return c;
        }

        public override void Send(Message m)
        {
            GameServer.Instance.rakNet.Send(m.Stream, m.Priority, m.Reliability, (char)0, UniqueID, false);
        }
    }
}
