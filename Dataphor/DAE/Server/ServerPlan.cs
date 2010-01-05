﻿/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Server
{
	public abstract class ServerPlan : ServerChildObject, IServerPlanBase, IServerPlan
	{        
		protected internal ServerPlan(ServerProcess AProcess) : base()
		{
			FProcess = AProcess;
			FPlan = new Plan(AProcess);
			FProgram = new Program(AProcess, FID);
			FProgram.ShouldPushLocals = true;
		}
		
		private bool FDisposed;
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					if (FActiveCursor != null)
						FActiveCursor.Dispose();
				}
				finally
				{
					if (FPlan != null)
					{
						FPlan.Dispose();
						FPlan = null;
					}
					
					if (!FDisposed)
						FDisposed = true;
				}
			}
			finally
			{
				FProgram = null;
				FProcess = null;
	            
				base.Dispose(ADisposing);
			}
		}

		// ID		
		private Guid FID = Guid.NewGuid();
		public Guid ID { get { return FID; } }
		
		// CachedPlanHeader
		private CachedPlanHeader FHeader;
		public CachedPlanHeader Header
		{ 
			get { return FHeader; } 
			set { FHeader = value; }
		}
		
		// PlanCacheTimeStamp
		public long PlanCacheTimeStamp; // Server.PlanCacheTimeStamp when the plan was compiled

		// Plan		
		protected Plan FPlan;
		public Plan Plan { get { return FPlan; } }
		
		// Program
		protected Program FProgram;
		public Program Program { get { return FProgram; } }
		
		// Statistics
		public PlanStatistics PlanStatistics { get { return FPlan.Statistics; } }
		public ProgramStatistics ProgramStatistics { get { return FProgram.Statistics; } }
		
		// Process        
		protected ServerProcess FProcess;
		public ServerProcess ServerProcess { get { return FProcess; } }
		
		public IServerProcess Process  { get { return (IServerProcess)FProcess; } }
		
		public CompilerMessages Messages { get { return FPlan.Messages; } }

		public void BindToProcess(ServerProcess AProcess)
		{
			FProcess = AProcess;
			FPlan.BindToProcess(AProcess);
			FProgram.BindToProcess(AProcess, FPlan);
		}
		
		public void UnbindFromProcess()
		{
			FProcess = null;
			FPlan.UnbindFromProcess();
			FProgram.UnbindFromProcess();
		}
		
		// Released
		/// <summary>
		/// Used to indicate that the plan has been released back to the cache and should be considered disposed for anything looking at the plan external to the server.
		/// </summary>
		public event EventHandler Released;
		
		protected void DoReleased()
		{
			if (Released != null)
				Released(this, EventArgs.Empty);
		}
		
		public void NotifyReleased()
		{
			DoReleased();
		}

		// ActiveCursor
		protected ServerCursor FActiveCursor;
		public ServerCursor ActiveCursor { get { return FActiveCursor; } }
		
		public void SetActiveCursor(ServerCursor AActiveCursor)
		{
			if (FActiveCursor != null)
				throw new ServerException(ServerException.Codes.PlanCursorActive);
			FActiveCursor = AActiveCursor;
			//FProcess.SetActiveCursor(FActiveCursor);
		}
		
		public void ClearActiveCursor()
		{
			FActiveCursor = null;
			//FProcess.ClearActiveCursor();
		}
		
		public virtual Statement EmitStatement()
		{
			CheckCompiled();
			return FProgram.Code.EmitStatement(EmitMode.ForCopy);
		}

		public void WritePlan(System.Xml.XmlWriter AWriter)
		{
			CheckCompiled();
			FProgram.Code.WritePlan(AWriter);
		}
		
		protected Exception WrapException(Exception AException)
		{
			return FProcess.ServerSession.WrapException(AException);
		}
		
		public void CheckCompiled()
		{
			FPlan.CheckCompiled();
		}
	}
	
	// ServerPlans    
	public class ServerPlans : ServerChildObjects
	{		
		protected override void Validate(ServerChildObject AObject)
		{
			if (!(AObject is ServerPlan))
				throw new ServerException(ServerException.Codes.ServerPlanContainer);
		}
		
		public new ServerPlan this[int AIndex]
		{
			get { return (ServerPlan)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(Guid APlanID)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].ID == APlanID)
					return LIndex;
			return -1;
		}
		
		public bool Contains(Guid APlanID)
		{
			return IndexOf(APlanID) >= 0;
		}
		
		public ServerPlan this[Guid APlanID]
		{
			get
			{
				int LIndex = IndexOf(APlanID);
				if (LIndex >= 0)
					return this[LIndex];
				throw new ServerException(ServerException.Codes.PlanNotFound, APlanID);
			}
		}
	}

	// ServerStatementPlan	
	public class ServerStatementPlan : ServerPlan, IServerStatementPlan
	{
		public ServerStatementPlan(ServerProcess AProcess) : base(AProcess) {}
		
		public void Execute(DataParams AParams)
		{
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				FProgram.Execute(AParams);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
	}
	
	// ServerExpressionPlan
	public class ServerExpressionPlan : ServerPlan, IServerExpressionPlan
	{
		protected internal ServerExpressionPlan(ServerProcess AProcess) : base(AProcess) {}
		
		public Schema.IDataType DataType
		{
			get
			{
				CheckCompiled();
				return FProgram.DataType;
			}
		}
		
		public Schema.IDataType ActualDataType
		{
			get
			{
				CheckCompiled();
				return FProgram.Code.DataType;
			}
		}
		
		public DataValue Evaluate(DataParams AParams)
		{
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				object LResult = FProgram.Execute(AParams);
				DataValue LDataValue = LResult as DataValue;
				if (LDataValue != null)
					return LDataValue;

				return DataValue.FromNative(FProgram.ValueManager, FProgram.DataType, LResult);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(LException);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		public IServerCursor Open(DataParams AParams)
		{
			IServerCursor LServerCursor;
			//ServerProcess.RaiseTraceEvent(TraceCodes.BeginOpenCursor, "Begin Open Cursor");
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();

				#if TIMING
				DateTime LStartTime = DateTime.Now;
				System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerExpressionPlan.Open", DateTime.Now.ToString("hh:mm:ss.ffff")));
				#endif
				ServerCursor LCursor = new ServerCursor(this, FProgram, AParams);
				try
				{
					LCursor.Open();
					#if TIMING
					System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerExpressionPlan.Open -- Open Time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));
					#endif
					LServerCursor = (IServerCursor)LCursor;
				}
				catch
				{
					Close((IServerCursor)LCursor);
					throw;
				}
			}
			catch (Exception E)
			{
				if (Header != null)
					Header.IsInvalidPlan = true;

				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
			//ServerProcess.RaiseTraceEvent(TraceCodes.EndOpenCursor, "End Open Cursor");
			return LServerCursor;
		}
		
		public void Close(IServerCursor ACursor)
		{
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				((ServerCursor)ACursor).Dispose();
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		private Schema.TableVar FTableVar;
		Schema.TableVar IServerExpressionPlan.TableVar 
		{ 
			get 
			{ 
				CheckCompiled();
				if (FTableVar == null)
					FTableVar = (Schema.TableVar)FPlan.PlanCatalog[Schema.Object.NameFromGuid(ID)];
				return FTableVar; 
			} 
		}
		
		Schema.Catalog IServerExpressionPlan.Catalog { get { return FPlan.PlanCatalog; } }
		
		public Row RequestRow()
		{
			CheckCompiled();
			return new Row(FProcess.ValueManager, SourceNode.DataType.RowType);
		}
		
		public void ReleaseRow(Row ARow)
		{
			CheckCompiled();
			ARow.Dispose();
		}
		
		public override Statement EmitStatement()
		{
			CheckCompiled();
			return FProgram.Code.EmitStatement(EmitMode.ForRemote);
		}

		// SourceNode
		internal TableNode SourceNode { get { return (TableNode)FProgram.Code.Nodes[0]; } }
		
		internal CursorNode CursorNode { get { return (CursorNode)FProgram.Code; } }
        
		// Isolation
		public CursorIsolation Isolation 
		{
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorIsolation; 
			}
		}
		
		// CursorType
		public CursorType CursorType 
		{ 
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorType; 
			} 
		}

		// Capabilities		
		public CursorCapability Capabilities 
		{ 
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorCapabilities; 
			} 
		}

		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}
		
		// Order
		public Schema.Order Order
		{
			get
			{
				CheckCompiled();
				return SourceNode.Order;
			}
		}
	}
}
