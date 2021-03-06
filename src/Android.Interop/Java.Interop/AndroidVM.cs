﻿using System;
using System.Collections.Generic;
using System.Reflection;

using Android.Runtime;

namespace Java.Interop {

	// FOR TEST PURPOSES ONLY
	public  delegate    IntPtr              SafeHandleDelegate_CallObjectMethodA    (IntPtr   env,    IntPtr    instance,   IntPtr method, JniArgumentValue[]    args);
	public  delegate    void                SafeHandleDelegate_DeleteLocalRef       (IntPtr   env,    IntPtr    handle);

	delegate int JNIEnv_GetJavaVM (IntPtr jnienv, out IntPtr vm);


	class AndroidVMBuilder : JniRuntime.CreationOptions {

		public AndroidVMBuilder ()
		{
			var GetJavaVM = (JNIEnv_GetJavaVM) Delegate.CreateDelegate (
					typeof(JNIEnv_GetJavaVM),
					typeof(JNIEnv).GetMethod ("GetJavaVM", BindingFlags.NonPublic | BindingFlags.Static));
			IntPtr invocationPointer;
			GetJavaVM (JNIEnv.Handle, out invocationPointer);

			EnvironmentPointer   = JNIEnv.Handle;
			NewObjectRequired   = ((int) Android.OS.Build.VERSION.SdkInt) <= 10;
			InvocationPointer   = invocationPointer;
			ObjectReferenceManager      = new AndroidObjectReferenceManager ();
			TypeManager                 = new AndroidTypeManager ();
		}

		public AndroidVM CreateAndroidVM ()
		{
			return new AndroidVM (this);
		}
	}

	class AndroidTypeManager : JniRuntime.JniTypeManager {

		protected override IEnumerable<Type> GetTypesForSimpleReference (string jniSimpleReference)
		{
			foreach (var t in base.GetTypesForSimpleReference (jniSimpleReference))
				yield return t;
			Type target = ((AndroidVM) Runtime).GetTypeMapping (jniSimpleReference);
			if (target != null)
				yield return target;
		}
	}

	class AndroidObjectReferenceManager : JniRuntime.JniObjectReferenceManager {
		public override int GlobalReferenceCount {
			get {return Java.Interop.Runtime.GlobalReferenceCount;}
		}

		public override int WeakGlobalReferenceCount {
			get {return 0;}
		}
	}

	public class AndroidVM : JniRuntime {

		internal AndroidVM (AndroidVMBuilder builder)
			: base (builder)
		{
		}

		static  readonly    AndroidVM   current = new AndroidVMBuilder ().CreateAndroidVM ();

		public static new AndroidVM Current {
			get {return current;}
		}

		Dictionary<string, Type> typeMappings   = new Dictionary<string, Type> ();

		public void AddTypeMapping (string jniTypeReference, Type type)
		{
			lock (typeMappings) {
				typeMappings [jniTypeReference] = type;
			}
		}

		internal Type GetTypeMapping (string jniSimpleReference)
		{
			Type target;
			lock (typeMappings) {
				if (typeMappings.TryGetValue (jniSimpleReference, out target))
					return target;
			}
			return null;
		}
	}
}

