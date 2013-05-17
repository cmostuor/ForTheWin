/* ----------------------------------------------------------------------------
 * This file was automatically generated by SWIG (http://www.swig.org).
 * Version 2.0.9
 *
 * Do not make changes to this file unless you know what you are doing--modify
 * the SWIG interface file instead.
 * ----------------------------------------------------------------------------- */

namespace RakNet {

using System;
using System.Runtime.InteropServices;

public class PublicKey : IDisposable {
  private HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal PublicKey(IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new HandleRef(this, cPtr);
  }

  internal static HandleRef getCPtr(PublicKey obj) {
    return (obj == null) ? new HandleRef(null, IntPtr.Zero) : obj.swigCPtr;
  }

  ~PublicKey() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          RakNetPINVOKE.delete_PublicKey(swigCPtr);
        }
        swigCPtr = new HandleRef(null, IntPtr.Zero);
      }
      GC.SuppressFinalize(this);
    }
  }

  public PublicKeyMode publicKeyMode {
    set {
      RakNetPINVOKE.PublicKey_publicKeyMode_set(swigCPtr, (int)value);
    } 
    get {
      PublicKeyMode ret = (PublicKeyMode)RakNetPINVOKE.PublicKey_publicKeyMode_get(swigCPtr);
      return ret;
    } 
  }

  public string remoteServerPublicKey {
    set {
      RakNetPINVOKE.PublicKey_remoteServerPublicKey_set(swigCPtr, value);
    } 
    get {
      string ret = RakNetPINVOKE.PublicKey_remoteServerPublicKey_get(swigCPtr);
      return ret;
    } 
  }

  public string myPublicKey {
    set {
      RakNetPINVOKE.PublicKey_myPublicKey_set(swigCPtr, value);
    } 
    get {
      string ret = RakNetPINVOKE.PublicKey_myPublicKey_get(swigCPtr);
      return ret;
    } 
  }

  public string myPrivateKey {
    set {
      RakNetPINVOKE.PublicKey_myPrivateKey_set(swigCPtr, value);
    } 
    get {
      string ret = RakNetPINVOKE.PublicKey_myPrivateKey_get(swigCPtr);
      return ret;
    } 
  }

  public PublicKey() : this(RakNetPINVOKE.new_PublicKey(), true) {
  }

}

}
