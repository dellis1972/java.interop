using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Text;

using Mono.Linq.Expressions;

using Java.Interop;

namespace Java.Interop.Dynamic {

	class JavaFieldInfo : JavaMemberInfo {

		public  string  JniSignature;

		JniPeerMembers  members;
		bool            isStatic;

		public JavaFieldInfo (JniPeerMembers members, string jniSignature, bool isStatic)
		{
			this.members    = members;
			this.isStatic   = isStatic;
			JniSignature    = jniSignature;
		}

		public override bool IsStatic {
			get {return isStatic;}
		}

		public override string Name {
			get {
				var i = JniSignature.IndexOf ('\u0000');
				return JniSignature.Substring (0, i);
			}
		}

		public object GetValue (IJavaObject self)
		{
			AssertSelf (self);

			if (IsStatic)
				return GetStaticValue ();
			return GetInstanceValue (self);
		}

		void AssertSelf (IJavaObject self)
		{
			if (IsStatic && self != null)
				throw new ArgumentException (
						string.Format ("Field '{0}' is static but an instance was provided.", JniSignature),
						"self");
			if (!IsStatic && self == null)
				throw new ArgumentException (
						string.Format ("Field '{0}' is an instance field but no instance was provided.", JniSignature),
						"self");
		}

		object GetStaticValue ()
		{
			var n   = GetSignatureStartIndex ();
			switch (JniSignature [n + 1]) {
			case 'Z':   return members.StaticFields.GetBooleanValue (JniSignature);
			case 'B':   return members.StaticFields.GetByteValue (JniSignature);
			case 'C':   return members.StaticFields.GetCharValue (JniSignature);
			case 'S':   return members.StaticFields.GetInt16Value (JniSignature);
			case 'I':   return members.StaticFields.GetInt32Value (JniSignature);
			case 'J':   return members.StaticFields.GetInt64Value (JniSignature);
			case 'F':   return members.StaticFields.GetSingleValue (JniSignature);
			case 'D':   return members.StaticFields.GetDoubleValue (JniSignature);
			case 'L':
			case '[':
				var lref = members.StaticFields.GetObjectValue (JniSignature);
				return ToReturnValue (lref, JniSignature, n + 1);
			default:
				throw new NotSupportedException ("Unsupported argument type: " + JniSignature.Substring (n + 1));
			}
		}

		object GetInstanceValue (IJavaObject self)
		{
			var n   = GetSignatureStartIndex ();
			switch (JniSignature [n + 1]) {
			case 'Z':   return members.InstanceFields.GetBooleanValue (JniSignature, self);
			case 'B':   return members.InstanceFields.GetByteValue (JniSignature, self);
			case 'C':   return members.InstanceFields.GetCharValue (JniSignature, self);
			case 'S':   return members.InstanceFields.GetInt16Value (JniSignature, self);
			case 'I':   return members.InstanceFields.GetInt32Value (JniSignature, self);
			case 'J':   return members.InstanceFields.GetInt64Value (JniSignature, self);
			case 'F':   return members.InstanceFields.GetSingleValue (JniSignature, self);
			case 'D':   return members.InstanceFields.GetDoubleValue (JniSignature, self);
			case 'L':
			case '[':
				var lref = members.InstanceFields.GetObjectValue (JniSignature, self);
				return ToReturnValue (lref, JniSignature, n + 1);
			default:
				throw new NotSupportedException ("Unsupported argument type: " + JniSignature.Substring (n + 1));
			}
		}

		int GetSignatureStartIndex ()
		{
			int n = JniSignature.IndexOf ('\u0000');
			if (n == JniSignature.Length - 1)
				throw new NotSupportedException (
						string.Format ("Could not determine field type from signature '{0}'.", JniSignature));
			return n;
		}

		public void SetValue (IJavaObject self, object value)
		{
			AssertSelf (self);

			if (IsStatic) {
				SetStaticValue (value);
			} else {
				SetInstanceValue (self, value);
			}
		}

		void SetStaticValue (object value)
		{
			var n   = GetSignatureStartIndex ();
			switch (JniSignature [n + 1]) {
			case 'Z':   members.StaticFields.SetValue (JniSignature, (bool)   value);   break;
			case 'B':   members.StaticFields.SetValue (JniSignature, (byte)   value);   break;
			case 'C':   members.StaticFields.SetValue (JniSignature, (char)   value);   break;
			case 'S':   members.StaticFields.SetValue (JniSignature, (short)  value);   break;
			case 'I':   members.StaticFields.SetValue (JniSignature, (int)    value);   break;
			case 'J':   members.StaticFields.SetValue (JniSignature, (long)   value);   break;
			case 'F':   members.StaticFields.SetValue (JniSignature, (float)  value);   break;
			case 'D':   members.StaticFields.SetValue (JniSignature, (double) value);   break;
			case 'L':
			case '[':
				using (var lref = JniMarshal.CreateLocalRef (value)) {
					members.StaticFields.SetValue (JniSignature, lref);
				}
				return;
			default:
				throw new NotSupportedException ("Unsupported argument type: " + JniSignature.Substring (n + 1));
			}
		}

		void SetInstanceValue (IJavaObject self, object value)
		{
			var n   = GetSignatureStartIndex ();
			switch (JniSignature [n + 1]) {
			case 'Z':   members.InstanceFields.SetValue (JniSignature, self,    (bool)   value);   break;
			case 'B':   members.InstanceFields.SetValue (JniSignature, self,    (byte)   value);   break;
			case 'C':   members.InstanceFields.SetValue (JniSignature, self,    (char)   value);   break;
			case 'S':   members.InstanceFields.SetValue (JniSignature, self,    (short)  value);   break;
			case 'I':   members.InstanceFields.SetValue (JniSignature, self,    (int)    value);   break;
			case 'J':   members.InstanceFields.SetValue (JniSignature, self,    (long)   value);   break;
			case 'F':   members.InstanceFields.SetValue (JniSignature, self,    (float)  value);   break;
			case 'D':   members.InstanceFields.SetValue (JniSignature, self,    (double) value);   break;
			case 'L':
			case '[':
				using (var lref = JniMarshal.CreateLocalRef (value)) {
					members.InstanceFields.SetValue (JniSignature, self, lref);
				}
				return;
			default:
				throw new NotSupportedException ("Unsupported argument type: " + JniSignature.Substring (n + 1));
			}
		}
	}
}