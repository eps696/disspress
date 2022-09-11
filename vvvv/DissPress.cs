#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "DissPress", Category = "String", Help = "dissociated press", Tags = "")]
	#endregion PluginInfo
	public class StringDissPressNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
	{
		#region fields & pins
		[Input("Input", DefaultString = "some text")]
		public IDiffSpread<string> SInput;

		[Input("Chunk", MinValue=1, DefaultValue=4)]
		public IDiffSpread<int> IChunk;

		[Input("Overlap", MinValue=2, DefaultValue=3)]
		public IDiffSpread<int> ILap;

		[Input("Steps", IsSingle=true, MinValue=2, DefaultValue=99)]
		public IDiffSpread<int> IStep;

		[Input("Enabled", IsSingle=true, DefaultValue=1)]
		public IDiffSpread<bool> BEnabled;

		[Output("Output", IsSingle=true)]
		public ISpread<string> SOutput;

		[Output("Not Found")]
		public ISpread<int> IOutSkip;

		int[] iPos, iLen, iChLp, cbad;

		[Import()]
		public ILogger FLogger;
		
		private readonly Spread<Task> FTasks = new Spread<Task>();
		private readonly Spread<CancellationTokenSource> FCts = new Spread<CancellationTokenSource>();
		private readonly Spread<CancellationToken> ct = new Spread<CancellationToken>();
		private int TaskCount = 0;
		
		#endregion fields & pins

		public void OnImportsSatisfied() {}
		public void Dispose()
		{
			for (int i = 0; i < TaskCount; i++) {
				int index = i;
//				FLogger.Log(LogType.Message, "Dispose task:"+index);
				CancelRunningTasks(index);
			}
		}
		private void CancelRunningTasks(int index)
		{
			if (FCts[index] != null) {
				FCts[index].Cancel();
				FCts[index].Dispose();
				FCts[index] = null;
			}
		}

		public void Evaluate(int SpreadMax)
		{
			SOutput.SliceCount = 1;
			IOutSkip.SliceCount = SpreadMax;

			FTasks.SliceCount = 1;
			FCts.SliceCount = 1;
			ct.SliceCount = 1;
			TaskCount = 1;
			
			this.iPos = new int[SpreadMax];
			this.iLen = new int[SpreadMax];
			this.iChLp = new int[SpreadMax];
			this.cbad = new int[SpreadMax];

			string[] sSrc = new string[SpreadMax];
			int[] iLap = new int[SpreadMax];
			
			for(int j=0; j<SpreadMax; j++)
			{
				iPos[j] = 0;
				cbad[j] = 0;
				iLen[j] = SInput[j%SInput.SliceCount].Length;
				iChLp[j] = IChunk[j%IChunk.SliceCount] + ILap[j%ILap.SliceCount];
				iLap[j] = ILap[j%ILap.SliceCount];
				sSrc[j] = SInput[j%SInput.SliceCount];
			}
// ========================================================
			if (BEnabled[0]) {
				int index = 0; // we have only one task to do
//				if (FCancel[index]) CancelRunningTasks(index);
				if (BEnabled.IsChanged || SInput.IsChanged || IChunk.IsChanged || ILap.IsChanged || IStep.IsChanged ) {
						
					CancelRunningTasks(index);
					FCts[index] = new CancellationTokenSource();
					ct[index] = FCts[index].Token;		
					
					FTasks[index] = Task.Factory.StartNew(() =>
					{
						ct[index].ThrowIfCancellationRequested();
						return new { Mix = DissPress(sSrc, iChLp, iLap, IStep[0], SpreadMax) };
					},
					ct[index]
					).ContinueWith(t =>
					{
						SOutput[0] = t.Result.Mix;
					},
					ct[index],
					TaskContinuationOptions.OnlyOnRanToCompletion,
					TaskScheduler.FromCurrentSynchronizationContext()
					);
				}
			} else {
				SOutput.AssignFrom(SInput);
			}
		}

		private string DissPress(string[] sInput, int[] iChLp, int[] iLap, int iSteps, int iMax) {
			int i=0, j=0, iOver, iFnd=0;
			string sLap="", sChnk="", sOver="", sOut="";
			bool search = false;

			for (int ii=0; ii<iSteps; ii++) {
				i = i%iMax;
				this.iPos[i] %= this.iLen[i];
				
// search for pattern from pointer, then from start; if no, go next file
				if (search) {
					for (int k=0; (k<iMax) ; k++) {
						j = (i+k)%iMax;
						iFnd = sInput[j].IndexOf(sLap, iPos[j]);
						if (iFnd < 0)
							iFnd = sInput[j].IndexOf(sLap, 0);
						if (iFnd >= 0) {
							iPos[j] = iFnd + iLap[j];
							i = j;
							break;
						} else {
							if (k == iMax-1)
								cbad[i] +=1;
						}
					}						
				}
// check if overflow, read chunk, move pointer
				iPos[i] %= iLen[i];
				iOver = iPos[i] + iChLp[i] - iLen[i];
				if ( iOver <= 0) {
					sChnk = sInput[i].Substring(iPos[i], iChLp[i]);
					iPos[i] += iChLp[i];
				}
				else {
					sOver = sInput[i] + sInput[i].Substring(0, iChLp[i]);
					sChnk = sOver.Substring(iPos[i], iChLp[i]);
					iPos[i] = iOver;
				}
// read pattern, add chunk to mix
				sLap = sChnk.Substring(iChLp[i]-iLap[i], iLap[i]);
				search = true;
					IOutSkip[i] = cbad[i];
				sOut += sChnk;
				i += 1;
			}
			return sOut;
		}
	}
}



