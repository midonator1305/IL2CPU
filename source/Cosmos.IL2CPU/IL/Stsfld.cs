using System;
using System.Linq;
using System.Reflection;
using XSharp;
using XSharp.Assembler;
using CPUx86 = XSharp.Assembler.x86;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Stsfld)]
  public class Stsfld : ILOp
  {
    public Stsfld(XSharp.Assembler.Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xType = aMethod.MethodBase.DeclaringType;
      var xOpCode = (ILOpCodes.OpField)aOpCode;
      FieldInfo xField = xOpCode.Value;
      var xIsReferenceType = IsReferenceType(xField.FieldType);

      // call cctor:
      var xCctor = (xField.DeclaringType.GetConstructors(BindingFlags.Static | BindingFlags.NonPublic)).SingleOrDefault();
      if (xCctor != null)
      {
        XS.Call(LabelName.Get(xCctor));
        ILOp.EmitExceptionLogic(Assembler, aMethod, aOpCode, true, null, ".AfterCCTorExceptionCheck");
        XS.Label(".AfterCCTorExceptionCheck");
      }

      //int aExtraOffset;// = 0;
      //bool xNeedsGC = xField.FieldType.IsClass && !xField.FieldType.IsValueType;
      uint xSize = SizeOfType(xField.FieldType);
      //if( xNeedsGC )
      //{
      //    aExtraOffset = 12;
      //}
      new Comment(Assembler, "Type = '" + xField.FieldType.FullName /*+ "', NeedsGC = " + xNeedsGC*/ );

      uint xOffset = 0;

      var xFields = xField.DeclaringType.GetFields();

      foreach (FieldInfo xInfo in xFields)
      {
        if (xInfo == xField)
          break;

        xOffset += SizeOfType(xInfo.FieldType);
      }
      string xDataName = DataMember.GetStaticFieldName(xField);
      if (xIsReferenceType)
      {
        XS.Add(XSRegisters.ESP, 4);
        XS.Pop(XSRegisters.EAX);
        XS.Set(ElementReference.New(xDataName).Name, XSRegisters.EAX, destinationIsIndirect: true, destinationDisplacement: 4);
        return;
      }
      for (int i = 0; i < (xSize / 4); i++)
      {
        XS.Pop(XSRegisters.EAX);
        new CPUx86.Mov { DestinationRef = ElementReference.New(xDataName, i * 4), DestinationIsIndirect = true, SourceReg = CPUx86.RegistersEnum.EAX };
      }
      switch (xSize % 4)
      {
        case 1:
          {
            XS.Pop(XSRegisters.EAX);
            new CPUx86.Mov { DestinationRef = ElementReference.New(xDataName, (int)((xSize / 4) * 4)), DestinationIsIndirect = true, SourceReg = CPUx86.RegistersEnum.AL };
            break;
          }
        case 2:
          {
            XS.Pop(XSRegisters.EAX);
            new CPUx86.Mov { DestinationRef = XSharp.Assembler.ElementReference.New(xDataName, (int)((xSize / 4) * 4)), DestinationIsIndirect = true, SourceReg = CPUx86.RegistersEnum.AX };
            break;
          }
        case 0:
          {
            break;
          }
        default:
          //EmitNotImplementedException(Assembler, GetServiceProvider(), "Ldsfld: Remainder size " + (xSize % 4) + " not supported!", mCurLabel, mMethodInformation, mCurOffset, mNextLabel);
          throw new NotImplementedException();
          //break;

      }
    }
  }


  // using System;
  // using System.Collections.Generic;
  // using Cosmos.IL2CPU.X86;
  //
  //
  // using CPUx86 = XSharp.Assembler.x86;
  // using System.Reflection;
  // using Cosmos.IL2CPU.Compiler;
  //
  // namespace Cosmos.IL2CPU.IL.X86 {
  // 	[XSharp.Assembler.OpCode(OpCodeEnum.Stsfld)]
  // 	public class Stsfld: Op {
  // 		private string mDataName;
  // 		private Type mDataType;
  // 		private bool mNeedsGC;
  // 		private string mBaseLabel;
  //         private string mNextLabel;
  // 	    private string mCurLabel;
  // 	    private uint mCurOffset;
  // 	    private MethodInformation mMethodInformation;
  //
  //         //public static void ScanOp(ILReader aReader, MethodInformation aMethodInfo, SortedList<string, object> aMethodData) {
  //         //    FieldInfo xField = aReader.OperandValueField;
  //         //    Engine.QueueStaticField(xField);
  //         //}
  //         private FieldInfo mField;
  // 		public Stsfld(ILReader aReader, MethodInformation aMethodInfo)
  // 			: base(aReader, aMethodInfo) {
  // 			mField = aReader.OperandValueField;
  //             mDataName = DataMember.GetStaticFieldName(mField);
  // 			mNeedsGC = !mField.FieldType.IsValueType;
  // 			mDataType = mField.FieldType;
  // 			mBaseLabel = GetInstructionLabel(aReader);
  //              mMethodInformation = aMethodInfo;
  // 		    mCurOffset = aReader.Position;
  // 		    mCurLabel = IL.Op.GetInstructionLabel(aReader);
  //             mNextLabel = IL.Op.GetInstructionLabel(aReader.NextPosition);
  // 		}
  //
  // 		public override void DoAssemble() {
  //             var xSize = GetService<IMetaDataInfoService>().SizeOfType(mField.FieldType);
  // 		    var xDecRefMethodInfo = GetService<IMetaDataInfoService>().GetMethodInfo(GCImplementationRefs.DecRefCountRef,
  // 		                                                                             false);
  //
  //
  // 			if (mNeedsGC) {
  //                 XS.Push(XSharp.Assembler.ElementReference.New(mDataName), isIndirect: true);
  //                 XS.Call(xDecRefMethodInfo.LabelName);
  // 			}
  //             for (int i = 0; i < (xSize / 4); i++)
  //             {
  //                 XS.Pop(XSRegisters.EAX);
  //                 new CPUx86.Move { DestinationRef = XSharp.Assembler.ElementReference.New(mDataName, i * 4), DestinationIsIndirect = true, SourceReg = CPUx86.Registers.EAX };
  // 			}
  //             switch (xSize % 4)
  //             {
  // 				case 1: {
  //                         XS.Pop(XSRegisters.EAX);
  //                         new CPUx86.Move { DestinationRef = XSharp.Assembler.ElementReference.New(mDataName, (int)((xSize / 4) * 4)), DestinationIsIndirect = true, SourceReg = CPUx86.Registers.AL };
  // 						break;
  // 					}
  // 				case 2: {
  //                         XS.Pop(XSRegisters.EAX);
  //                         new CPUx86.Move { DestinationRef = XSharp.Assembler.ElementReference.New(mDataName, (int)((xSize / 4) * 4)), DestinationIsIndirect = true, SourceReg = CPUx86.Registers.AX };
  //                         break;
  // 					}
  // 				case 0: {
  // 						break;
  // 					}
  // 				default:
  //                     EmitNotImplementedException(Assembler, GetServiceProvider(), "Ldsfld: Remainder size " + (xSize % 4) + " not supported!", mCurLabel, mMethodInformation, mCurOffset, mNextLabel);
  //                     break;
  //
  // 			}
  // 			Assembler.Stack.Pop();
  // 		}
  // 	}
  // }

}
