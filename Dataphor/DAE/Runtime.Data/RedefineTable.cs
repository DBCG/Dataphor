/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
	using System.Collections;
	using System.Diagnostics;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Schema = Alphora.Dataphor.DAE.Schema;

    public class RedefineTable : Table
    {
		public RedefineTable(RedefineNode ANode, Program AProgram) : base(ANode, AProgram){}

        public new RedefineNode Node { get { return (RedefineNode)FNode; } }
        
		protected Table FSourceTable;
		protected Row FSourceRow;
        
        protected override void InternalOpen()
        {
			FSourceTable = Node.Nodes[0].Execute(Program) as Table;
			try
			{
				FSourceRow = new Row(Manager, FSourceTable.DataType.RowType);
			}
			catch
			{
				FSourceTable.Dispose();
				FSourceTable = null;
				throw;
			}
        }
        
        protected override void InternalClose()
        {
			if (FSourceTable != null)
			{
				FSourceTable.Dispose();
				FSourceTable = null;
			}

            if (FSourceRow != null)
            {
				FSourceRow.Dispose();
                FSourceRow = null;
            }
        }
        
        protected override void InternalReset()
        {
            FSourceTable.Reset();
        }
        
        protected override void InternalSelect(Row ARow)
        {
            FSourceTable.Select(FSourceRow);

			int LColumnIndex;            
            for (int LIndex = 0; LIndex < Node.DataType.Columns.Count; LIndex++)
            {
				if (!((IList)Node.RedefineColumnOffsets).Contains(LIndex))
				{
					LColumnIndex = ARow.DataType.Columns.IndexOfName(DataType.Columns[LIndex].Name);
					if (LColumnIndex >= 0)
						if (FSourceRow.HasValue(LIndex))
							ARow[LColumnIndex] = FSourceRow[LIndex];
						else
							ARow.ClearValue(LColumnIndex);
				}
            }

	        Program.Stack.Push(FSourceRow);
            try
            {
				for (int LIndex = 0; LIndex < Node.RedefineColumnOffsets.Length; LIndex++)
				{
					LColumnIndex = ARow.DataType.Columns.IndexOfName(Node.DataType.Columns[Node.RedefineColumnOffsets[LIndex]].Name);
					if (LColumnIndex >= 0)
					{
						ARow[LColumnIndex] = Node.Nodes[LIndex + 1].Execute(Program);
					}
				}
            }
            finally
            {
				Program.Stack.Pop();
            }
        }
        
        protected override bool InternalNext()
        {
            return FSourceTable.Next();
        }
        
        protected override bool InternalBOF()
        {
            return FSourceTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
            return FSourceTable.EOF();
        }
    }
}