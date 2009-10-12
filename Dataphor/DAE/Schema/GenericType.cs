/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Device.Catalog;

namespace Alphora.Dataphor.DAE.Schema
{
	public interface IGenericType : IDataType {}
	
    public class GenericType : IGenericType
    {
		public GenericType() : base()
		{
			FIsGeneric = true;
		}
		
		public GenericType(bool AIsNil) : base()
		{
			FIsGeneric = true;
			FIsNil = AIsNil;
		}
		
		public override bool Equals(object AObject)
		{
			return (AObject is IDataType) && Equals((IDataType)AObject);
		}
		
		public override int GetHashCode()
		{
			return 0;
		}
		
		public bool Equivalent(IDataType ADataType)
		{
			return Equals(ADataType);
		}

		public bool Equals(IDataType ADataType)
		{
			return ADataType is IGenericType;
		}

		// Is
		public bool Is(IDataType ADataType)
		{
			return ADataType is GenericType;
		}
		
		public bool Compatible(IDataType ADataType)
		{
			return Is(ADataType) || ADataType.Is(this);
		}
		
		public string Name
		{
			get { return Schema.DataTypes.CSystemGeneric; }
			set { }
		}

		#if NATIVEROW
		public int GetByteSize(object AData)
		{
			return 0;
		}
		#else
		// StaticByteSize		
		private int FStaticByteSize = 0;
		public int StaticByteSize
		{
			get { return FStaticByteSize; }
			set { }
		}
		#endif
		
		// IsGeneric
		// Indicates whether this data type is a generic data type (i.e. table, not table{})
		private bool FIsGeneric;
		public bool IsGeneric
		{
			get { return FIsGeneric; }
			set { FIsGeneric = value; }
		}
		
		// IsNil
		// True if the type is known to be the constant nil at compile-time
		private bool FIsNil;
		public bool IsNil { get { return FIsNil; } }
		
		// IsDisposable
		// Indicates whether the host representation for this data type must be disposed
		private bool FIsDisposable = false;
		public bool IsDisposable
		{
			get { return FIsDisposable; }
			set { FIsDisposable = value; }
		}

        public TypeSpecifier EmitSpecifier(EmitMode AMode)
        {
			return new GenericTypeSpecifier();
        }
        
        public void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
        }
    }
}