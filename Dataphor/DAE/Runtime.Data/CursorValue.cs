/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Runtime;
	
	public class CursorValue : DataValue
	{
		public CursorValue(IServerProcess AProcess, Schema.ICursorType ACursorType, int AID) : base(AProcess, ACursorType)
		{
			FID = AID;
		}
		
		private int FID;
		public int ID { get { return FID; } }
		
		public override object AsNative
		{
			get { return FID; }
			set { FID = (int)value; }
		}
		
		public override bool IsNil { get { return false; } }

		public override int GetPhysicalSize(bool AExpandStreams)
		{
			return sizeof(int);
		}

		#if USE_UNSAFE 

		public unsafe override void WriteToPhysical(byte[] ABuffer, int AOffset, bool AExpandStreams)
		{
			fixed (byte* LBufferPtr = &(ABuffer[AOffset]))
			{
				*((int*)LBufferPtr) = FID;
			}
		}
		
		public unsafe override void ReadFromPhysical(byte[] ABuffer, int AOffset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[AOffset]))
			{
				FID = *((int*)LBufferPtr);
			}
		}

		#else

		public override void WriteToPhysical(byte[] ABuffer, int AOffset, bool AExpandStreams)
		{
			ByteArrayUtility.WriteInt32(ABuffer, AOffset, FID);
		}

		public override void ReadFromPhysical(byte[] ABuffer, int AOffset)
		{
			FID = ByteArrayUtility.ReadInt32(ABuffer, AOffset);
		}

		#endif
		
		public override object CopyNativeAs(Schema.IDataType ADataType)
		{
			return FID;
		}
	}

	// Cursors
    public class Cursor : Disposable
    {
		protected internal Cursor(CursorManager AManager, int AID, Table ATable) : base()
		{
			FID = AID;
			FTable = ATable;
			FManager = AManager;
			SnapshotContext(ATable.Process);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				FID = -1;
				
				try
				{
					if (FTable != null)
					{
						FTable.Dispose();
						FTable = null;
					}
				}
				finally
				{
					if (FContext != null)
						FContext = null;
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		protected Stack FContext;
		
		public void SnapshotContext(ServerProcess AProcess)
		{
			FContext = new Stack(AProcess.Stack.MaxStackDepth, AProcess.Stack.MaxCallDepth);
			for (int LIndex = AProcess.Stack.Count - 1; LIndex >= 0; LIndex--)
				FContext.Push(DataValue.CopyValue(AProcess, AProcess.Stack.Peek(LIndex)));
		}
		
		public void SwitchContext(ServerProcess AProcess)
		{
			FContext = AProcess.SwitchContext(FContext);
		}
		
		// ID
		protected int FID = -1;
		public int ID { get { return FID; } }
		
		// Table
		protected Table FTable;
		public Table Table { get { return FTable; } }
		
		// Manager
		protected CursorManager FManager;
 		public CursorManager Manager { get { return FManager; } }
    }
    
    public class Cursors : DisposableTypedList
    {		
		public Cursors() : base()
		{
			FItemsOwned = true;
			FItemType = typeof(Cursor);
		}
		
		public new Cursor this[int AIndex]
		{
			get { return (Cursor)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public Cursor GetCursor(int ACursorID)
		{
			foreach (Cursor LCursor in this)
				if (LCursor.ID == ACursorID)
					return LCursor;
			throw new RuntimeException(RuntimeException.Codes.CursorNotFound, ACursorID.ToString());
		}
    }
    
    public class CursorManager : Disposable
    {
		public CursorManager() : base(){}
		
		protected int FNextCursorID = -1;
		protected Cursors FCursors = new Cursors();
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FCursors != null) 
				{
					while (FCursors.Count > 0)
						FCursors[0].Dispose();
					FCursors.Dispose();
					FCursors = null;
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		public int CreateCursor(Table ATable)
		{
			ATable.Open(); // JIC
			FNextCursorID++;
			Cursor LCursor = new Cursor(this, FNextCursorID, ATable);
			try
			{
				FCursors.Add(LCursor);
				return LCursor.ID;
			}
			catch
			{
				LCursor.Dispose();
				throw;
			}
		}
		
		public Cursor GetCursor(int ACursorID)
		{
			return FCursors.GetCursor(ACursorID);
		}
		
		public void CloseCursor(int ACursorID)
		{
			GetCursor(ACursorID).Dispose();
		}
	}
}