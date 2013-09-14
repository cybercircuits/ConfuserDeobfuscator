﻿/*
    Copyright (C) 2011-2013 de4dot@gmail.com

    This file is part of de4dot.

    de4dot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    de4dot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with de4dot.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using de4dot.blocks;

namespace de4dot.code.deobfuscators {
	abstract class ValueInlinerBase<TValue> : MethodReturnValueInliner {
		MethodDefAndDeclaringTypeDict<Func<MethodDef, MethodSpec, object[], object>> decrypterMethods = new MethodDefAndDeclaringTypeDict<Func<MethodDef, MethodSpec, object[], object>>();
		bool removeUnbox = false;

		class MyCallResult : CallResult {
			public IMethod methodRef;
			public MethodSpec gim;
			public MyCallResult(Block block, int callEndIndex, IMethod method, MethodSpec gim)
				: base(block, callEndIndex) {
				this.methodRef = method;
				this.gim = gim;
			}
		}

		public bool RemoveUnbox {
			get { return removeUnbox; }
			set { removeUnbox = value; }
		}

		public override bool HasHandlers {
			get { return decrypterMethods.Count != 0; }
		}

		public IEnumerable<MethodDef> Methods {
			get { return decrypterMethods.getKeys(); }
		}

		public void add(MethodDef method, Func<MethodDef, MethodSpec, object[], object> handler) {
			if (method == null)
				return;
			if (decrypterMethods.find(method) != null)
				throw new ApplicationException(string.Format("Handler for method {0:X8} has already been added", method.MDToken.ToInt32()));
			if (method != null)
				decrypterMethods.add(method, handler);
		}

		protected override void inlineAllCalls() {
			foreach (var tmp in callResults) {
				var callResult = (MyCallResult)tmp;
				var handler = decrypterMethods.find(callResult.methodRef);
				callResult.returnValue = handler((MethodDef)callResult.methodRef, callResult.gim, callResult.args);
			}
		}

		protected override CallResult createCallResult(IMethod method, MethodSpec gim, Block block, int callInstrIndex) {
			if (decrypterMethods.find(method) == null)
				return null;
			return new MyCallResult(block, callInstrIndex, method, gim);
		}

		protected bool removeUnboxInstruction(Block block, int index, string unboxType) {
			if (!removeUnbox)
				return false;
			var instrs = block.Instructions;
			if (index >= instrs.Count)
				return false;
			var unbox = instrs[index];
			if (unbox.OpCode.Code != Code.Unbox_Any)
				return false;
			var type = unbox.Operand as ITypeDefOrRef;
			if (type == null || type.FullName != unboxType)
				return false;
			block.remove(index, 1);
			return true;
		}
	}

	class BooleanValueInliner : ValueInlinerBase<bool> {
		protected override void inlineReturnValues(IList<CallResult> callResults) {
			foreach (var callResult in callResults) {
				var block = callResult.block;
				int num = callResult.callEndIndex - callResult.callStartIndex + 1;

				block.replace(callResult.callStartIndex, num, Instruction.CreateLdcI4((bool)callResult.returnValue ? 1 : 0));
				removeUnboxInstruction(block, callResult.callStartIndex + 1, "System.Boolean");
				Logger.v("Decrypted boolean: {0}", callResult.returnValue);
			}
		}
	}

	class Int32ValueInliner : ValueInlinerBase<int> {
		protected override void inlineReturnValues(IList<CallResult> callResults) {
			foreach (var callResult in callResults) {
				var block = callResult.block;
				int num = callResult.callEndIndex - callResult.callStartIndex + 1;

				block.replace(callResult.callStartIndex, num, Instruction.CreateLdcI4((int)callResult.returnValue));
				removeUnboxInstruction(block, callResult.callStartIndex + 1, "System.Int32");
				Logger.v("Decrypted int32: {0}", callResult.returnValue);
			}
		}
	}

	class Int64ValueInliner : ValueInlinerBase<long> {
		protected override void inlineReturnValues(IList<CallResult> callResults) {
			foreach (var callResult in callResults) {
				var block = callResult.block;
				int num = callResult.callEndIndex - callResult.callStartIndex + 1;

				block.replace(callResult.callStartIndex, num, OpCodes.Ldc_I8.ToInstruction((long)callResult.returnValue));
				removeUnboxInstruction(block, callResult.callStartIndex + 1, "System.Int64");
				Logger.v("Decrypted int64: {0}", callResult.returnValue);
			}
		}
	}

	class SingleValueInliner : ValueInlinerBase<float> {
		protected override void inlineReturnValues(IList<CallResult> callResults) {
			foreach (var callResult in callResults) {
				var block = callResult.block;
				int num = callResult.callEndIndex - callResult.callStartIndex + 1;

				block.replace(callResult.callStartIndex, num, OpCodes.Ldc_R4.ToInstruction((float)callResult.returnValue));
				removeUnboxInstruction(block, callResult.callStartIndex + 1, "System.Single");
				Logger.v("Decrypted single: {0}", callResult.returnValue);
			}
		}
	}

	class DoubleValueInliner : ValueInlinerBase<double> {
		protected override void inlineReturnValues(IList<CallResult> callResults) {
			foreach (var callResult in callResults) {
				var block = callResult.block;
				int num = callResult.callEndIndex - callResult.callStartIndex + 1;

				block.replace(callResult.callStartIndex, num, OpCodes.Ldc_R8.ToInstruction((double)callResult.returnValue));
				removeUnboxInstruction(block, callResult.callStartIndex + 1, "System.Double");
				Logger.v("Decrypted double: {0}", callResult.returnValue);
			}
		}
	}
}