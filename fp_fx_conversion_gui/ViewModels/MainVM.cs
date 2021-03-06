using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fp_fx_conversion_gui.ViewModels
{
	using GalaSoft.MvvmLight.Command;
	using GalaSoft.MvvmLight;
	using System.Windows.Input;
	using System.IO;
	using System.Diagnostics;
	using System.Windows;
	using System.Collections;
	using Helpers;
	using System.Numerics;
	public class MainVM : ObservableObject
	{
		public static string OutputDir = "output";

		public void GenerateModule()
		{
			Directory.CreateDirectory(OutputDir);
			Dictionary<string, string> files = new Dictionary<string, string>();
			if(Select_FP2Fixed)
			{
				if (Fp2Fx_TotalBits > 32)
				{
					StringBuilder sb = new StringBuilder(Resources.Res.float_to_fixed_64);
					sb.Replace(Resources.Res.V_TOT_BITS, Fp2Fx_TotalBits.ToString());
					sb.Replace(Resources.Res.V_Fp2Fx_Bit_Diff, (Fp2Fx_TotalBits - 32 - 1).ToString());

					files.Add("float_to_fixed.v", sb.ToString());
				}
				else
				{
					files.Add("float_to_fixed.v", Resources.Res.float_to_fixed.Replace(Resources.Res.V_TOT_BITS, Fp2Fx_TotalBits.ToString()));

				}

				files.Add("float_to_fixed_tb.v", Resources.Res.float_to_fixed_tb.Replace(Resources.Res.V_TOT_BITS, Fp2Fx_TotalBits.ToString()));

				GenerateFloatingPointData(Fp2Fx_TotalTestVectors, Path.Combine(OutputDir, Resources.Res.V_Fp2Fx_Data));
			}

			if (Select_Fixed2FP)
			{
				files.Add("fixed_to_float.v", Resources.Res.fixed_to_float.Replace(Resources.Res.V_TOT_BITS, Fx2Fp_TotalBits.ToString()));

				files.Add("fixed_to_float_tb.v", Resources.Res.fixed_to_float_tb.Replace(Resources.Res.V_TOT_BITS, Fx2Fp_TotalBits.ToString()));

				files.Add("scale_up.v", Resources.Res.scale_up);
				GenerateFixedPointData(Fx2Fp_TotalTestVectors, Fx2Fp_TotalBits, Path.Combine(OutputDir, Resources.Res.V_Fx2Fp_Data));
			}

			files.Add("twos_comp.v", Resources.Res.twos_comp);

			foreach (var fd in files)
			{
				string fn = Path.Combine(OutputDir, fd.Key);
				File.Delete(fn);
				File.AppendAllText(fn, fd.Value);
			}

		}

		static Random _seed = new Random((int)DateTime.Now.Ticks);
		public void GenerateFloatingPointData(int count, string filePath)
		{
			File.Delete(filePath);
			using (StreamWriter sw = File.CreateText(filePath))
			{
				Random r = new Random(_seed.Next());
				int lim = count / 4;
				List<float> values = new List<float>(count);
				for(int i = 0; i < lim; i++)

				{
					float val = (float)(((double)r.Next(-Int32.MaxValue/2048, Int32.MaxValue/2048))/(double)UInt32.MaxValue);
					values.Add(val);
				}
				
				for (int i = 0; i < lim; i++)
				{
					float val = (float)(((double)r.Next(-Int32.MaxValue / 256, Int32.MaxValue / 256)) / (double)UInt32.MaxValue);
					values.Add(val);

				}

				for (int i = 0; i < lim; i++)
				{
					float val = (float)(((double)r.Next(-Int32.MaxValue / 32, Int32.MaxValue / 32)) / (double)Int32.MaxValue);
					values.Add(val);
				}

				lim = count - lim * 3;
				for (int i = 0; i < lim; i++)
				{
					float val = (float)(((double)r.Next(-Int32.MaxValue, Int32.MaxValue)) / (double)Int32.MaxValue);
					values.Add(val);
				}

				foreach (var val in values)
				{
					byte[] fbytes = BitConverter.GetBytes(val);
					BitArray ba = new BitArray(fbytes);
					sw.WriteLine(ba.ToBitString());

				}
			}
		}

		public void GenerateFixedPointData(int count, int bits, string filePath)
		{
			File.Delete(filePath);
			using (StreamWriter sw = File.CreateText(filePath))
			{
				Random r = new Random(_seed.Next());
				int lim = count / 4;
				List<float> values = new List<float>(count);
				for (int i = 0; i < lim; i++)
				{
					float val = (float)(((double)r.Next(-Int32.MaxValue / 2048, Int32.MaxValue / 2048)) / (double)UInt32.MaxValue);
					values.Add(val);
				}

				for (int i = 0; i < lim; i++)
				{
					float val = (float)(((double)r.Next(-Int32.MaxValue / 256, Int32.MaxValue / 256)) / (double)UInt32.MaxValue);
					values.Add(val);

				}

				for (int i = 0; i < lim; i++)
				{
					float val = (float)(((double)r.Next(-Int32.MaxValue / 32, Int32.MaxValue / 32)) / (double)Int32.MaxValue);
					values.Add(val);
				}

				lim = count - lim * 3;
				for (int i = 0; i < lim; i++)
				{
					float val = (float)(((double)r.Next(-Int32.MaxValue, Int32.MaxValue)) / (double)Int32.MaxValue);
					values.Add(val);
				}

				foreach (var val in values)
				{
					long ival = (long)(val * Math.Pow(2,(bits - 1)));
					byte[] fbytes = BitConverter.GetBytes(ival);
					BigInteger bi = new BigInteger(1);
					BitArray ba = new BitArray(fbytes);
#if DEBUG
					sw.WriteLine(ba.ToBitString(bits));

					//sw.Write(ba.ToBitString(bits));
					//sw.WriteLine(":" + val.ToString());
#else
					sw.WriteLine(ba.ToBitString(bits));
#endif
				}
			}
		}

		public async Task<bool> CompileWithIcarus(string args)
		{
			Process process = new Process();
			ProcessStartInfo startInfo = new ProcessStartInfo();
			//startInfo.WorkingDirectory = Path.GetDirectoryName(filefullPath);
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.FileName = IcarusDir;
			startInfo.WorkingDirectory = Path.Combine(Environment.CurrentDirectory, "output");
			startInfo.Arguments = args;
			startInfo.CreateNoWindow = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo = startInfo;
			process.Start();
			string err = await process.StandardError.ReadToEndAsync();
			process.WaitForExit(5000);
			return string.IsNullOrEmpty(err) || string.IsNullOrWhiteSpace(err);
		}

		public void VerifyTestbenchWithIcarus(string args)
		{
			Process process = new Process();
			ProcessStartInfo startInfo = new ProcessStartInfo();
			//startInfo.WorkingDirectory = Path.GetDirectoryName(filefullPath);
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.FileName = Path.Combine(Path.GetDirectoryName(IcarusDir),"vvp.exe");
			startInfo.WorkingDirectory = Path.Combine(Environment.CurrentDirectory, "output");
			startInfo.Arguments = args;
			startInfo.CreateNoWindow = true;
			//startInfo.RedirectStandardOutput = true;
			startInfo.UseShellExecute = false;
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo = startInfo;
			process.Start();
			//string output = await process.StandardOutput.ReadLineAsync();
			process.WaitForExit(5000);
		}

		long twosComp(string value, int bits)
		{
			long fxVal = Convert.ToInt64(value, 2);
			BitArray ba = new BitArray(BitConverter.GetBytes(fxVal));
			//string ssss = ba.ToBitString(64);
			if (ba[value.Length - 1])
			{
				BitArray baf = new BitArray(64);
				for(int i=baf.Count-1;i>=bits ;i--)
				{
					baf[i] = true;
				}
				//ssss = baf.ToBitString(64);
				byte[] longB = new byte[8];
				//string bar = baf.Or(ba).ToBitString(64);
				baf.Or(ba).CopyTo(longB, 0);
				fxVal = BitConverter.ToInt64(longB, 0);
			}

			return fxVal;
		}

		//Defaut 32bit
		float GetFloatingPointFromBinary(string bin)
		{
			int float2int = Convert.ToInt32(bin, 2);
			byte[] intBytes = BitConverter.GetBytes(float2int);
			return BitConverter.ToSingle(intBytes, 0);
		}

		float GetFixedPointFromBinary(string bin, int bits)
		{
			long fxVal = twosComp(bin, bits);
			//long fxVal = Convert.ToInt64(vals[0], 2);
			//byte[] gb = BitConverter.GetBytes(fxVal);
			//long sss = BitConverter.ToInt64(gb, bits - 1);
			////double vv = FixedIntToFloat(fxVal, bits);
			return (float)fxVal / (float)Math.Pow(2, bits - 1);
		}

		float PercentCalc(double input, double output)
		{
			return (float)(100 * Math.Abs((input - output)/ input));
		}

		string numeric_format = "{0:N15}           {1:N15}           {2:N10}%    ";
		string numeric_format_csv = "{0:N15},{1:N15},{2:N10}";
		public void VerifyFixedToFloatValues(string filePath, int bits, string outPath)
		{
			if(File.Exists(filePath))
			{
				string header_format_csv = "Input_Fixed,Output_Float,Error";
				string header_format = "Input(Fixed)             Output(Float)            % Error   ";
				StringBuilder sb = new StringBuilder();

				//if(!GenerateMatlabAndCSV)	sb.AppendLine("Input File: " + filePath);
				string format = GenerateMatlabAndCSV ? numeric_format_csv : numeric_format;
				//string hformat = GenerateMatlabAndCSV ? header_format_csv : header_format;
				//sb.AppendLine(hformat);

				string[] lines = File.ReadAllLines(filePath);
				List<float> fixedIn = new List<float>();
				foreach (var l in lines)
				{
					string[] vals = l.Split(":".ToCharArray());
					float fxValFp = GetFixedPointFromBinary(vals[0], bits);
					fixedIn.Add(fxValFp);
					float fpVal = GetFloatingPointFromBinary(vals[1]);

					float percent = PercentCalc(fxValFp, fpVal);
					string added = string.Format(format, fxValFp, fpVal, percent);
					sb.AppendLine(added);
				}

				//string outPath = "output\\Result_Fixed_to_Float" + (GenerateMatlabAndCSV ? ".csv" : ".txt");
				File.Delete(outPath);
				using (StreamWriter sw = File.CreateText(outPath))
				{
					if (!GenerateMatlabAndCSV)
					{
						sw.WriteLine("Input File: " + filePath);
						sw.WriteLine(header_format);
					}
					sw.Write(sb.ToString());
				}

				if (!GenerateMatlabAndCSV)
					Process.Start(outPath);
			}
		}

		public void VerifyFloatToFixedValues(string filePath, int bits, string outPath)
		{
			if (File.Exists(filePath))
			{
				string header_format_csv = "Input_Float,Output_Fixed,Error";
				string header_format = "Input(Float)             Output(Fixed)            % Error   ";
				StringBuilder sb = new StringBuilder();
				//if (!GenerateMatlabAndCSV) sb.AppendLine("Input File: " + filePath);

				string format = GenerateMatlabAndCSV ? numeric_format_csv : numeric_format;
				//string hformat = GenerateMatlabAndCSV ? header_format_csv : header_format;
				//sb.AppendLine(hformat);

				string[] lines = File.ReadAllLines(filePath);
				List<float> fixedIn = new List<float>();
				foreach (var l in lines)
				{
					string[] vals = l.Split(":".ToCharArray());
					float fxValFp = GetFixedPointFromBinary(vals[1], bits);
					fixedIn.Add(fxValFp);
					float fpVal = GetFloatingPointFromBinary(vals[0]);

					float percent = PercentCalc(fxValFp, fpVal);

					string added = string.Format(format, fpVal, fxValFp, percent);
					sb.AppendLine(added);
				}

				//string outPath = "output\\Result_Float_to_Fixed" + (GenerateMatlabAndCSV ? ".csv" : ".txt");
				File.Delete(outPath);
				using (StreamWriter sw = File.CreateText(outPath))
				{
					if (!GenerateMatlabAndCSV)
					{
						sw.WriteLine("Input File: " + filePath);
						sw.WriteLine(header_format);
					}
					sw.Write(sb.ToString());
				}

				if(!GenerateMatlabAndCSV)
					Process.Start(outPath);
			}
		}

		void GenerateMatlab(string outPath)
		{
			File.Delete(outPath);
			StringBuilder sb = new StringBuilder(Resources.Res.matlab_file);
			if (Select_Fixed2FP)
			{
				sb.Replace("%==>>", "").Replace("__Fx2Fp__", Resources.Res.OutFile_Fx2Fp_NoExt);
			}
			if (Select_FP2Fixed)
			{
				sb.Replace("%-->>", "").Replace("__Fp2Fx__", Resources.Res.OutFile_Fp2Fx_NoExt);
			}
			using (StreamWriter sw = File.CreateText(outPath))
			{
				sw.Write(sb.ToString());
			}
		}

		public MainVM()
		{
			ExecuteIcarus = new RelayCommand(async () =>
			{
				Executing = true;
				ShowStatus = true;
				StatusString = "Generating Veriglog and Data Files...";
				await Task.Factory.StartNew(()=> { GenerateModule(); });
				StatusString = "Successfully Generated Veriglog and Data Files!";
				await Task.Delay(200);
				StatusString = "Compiling with iCarus...";

				string args = "";
				string w = Path.Combine(Environment.CurrentDirectory, "output");
				bool iCarusFx2Fp = false, iCarusFp2Fx = false;
				if (Select_Fixed2FP)
				{
					//Executing iCarus
					args = " -o \"<PATH>\\fixed_2_fp.vvp\" \"<PATH>\\fixed_to_float.v\" \"<PATH>\\twos_comp.v\" \"<PATH>\\scale_up.v\" \"<PATH>\\fixed_to_float_tb.v\"".Replace("<PATH>", w);
					iCarusFx2Fp = await CompileWithIcarus(args);

					//Executing vvp
					if(iCarusFx2Fp)
					{
						args = string.Format("\"{0}\"", Path.Combine(w, "fixed_2_fp.vvp"));
						await Task.Factory.StartNew(() => { VerifyTestbenchWithIcarus(args); }); ;
					}
					StatusString = "Fixed to Floating Compilation " + (iCarusFx2Fp?"Succeeded!":"Failed!");

					string inputPath = Path.Combine(w, "fx2fp_out.txt");
					string outPath = Path.Combine(w, Resources.Res.OutFile_Fx2Fp_NoExt + ".txt");

					await Task.Factory.StartNew(() => { VerifyFixedToFloatValues(inputPath, Fx2Fp_TotalBits, outPath); });
					await Task.Delay(500);
				}

				if (Select_FP2Fixed)
				{
					args = " -o \"<PATH>\\fp_2_fixed.vvp\" \"<PATH>\\float_to_fixed.v\" \"<PATH>\\twos_comp.v\" \"<PATH>\\float_to_fixed_tb.v\"".Replace("<PATH>", w);
					iCarusFp2Fx = await CompileWithIcarus(args);

					//Executing vvp
					if (iCarusFp2Fx)
					{
						args = string.Format("\"{0}\"", Path.Combine(w, "fp_2_fixed.vvp"));
						await Task.Factory.StartNew(() => { VerifyTestbenchWithIcarus(args); });
					}

					StatusString = "Floating to Fixed Compilation " + (iCarusFp2Fx ? "Succeeded!" : "Failed!");

					string inputPath = Path.Combine(w, "fp2fx_out.txt");
					string outPath = Path.Combine(w, Resources.Res.OutFile_Fp2Fx_NoExt + ".txt");
				
					await Task.Factory.StartNew(() => { VerifyFloatToFixedValues(inputPath, Fp2Fx_TotalBits, outPath); });

				}
				if(GenerateMatlabAndCSV)
				{
					await Task.Factory.StartNew(() => 
					{
						GenerateMatlab("output\\verify_float_fixed.m"); }
					);
					MessageBox.Show("Matlab file named: verify_float_fixed.m" + " written", "Matlab File",	MessageBoxButton.OK, MessageBoxImage.Information);
				}

				/*
				//Execute Icarus for Float to Fixed
				arg = " -o <PATH>\\fixed_2_fp.vvp <PATH>\\float_to_fixed.v <PATH>\\twos_comp.v <PATH>\\float_to_fixed_tb.v".Replace("<PATH>", (startInfo.WorkingDirectory));
				*/
				Executing = false;
			}, () => { return !Executing && Found_Icarus && (Select_Fixed2FP || Select_FP2Fixed); });

			Select_Fixed2FP = true;
			Select_FP2Fixed = true;
			Executing = false;
			Fp2Fx_TotalBits = Fx2Fp_TotalBits = 32;
			Fp2Fx_TotalTestVectors = Fx2Fp_TotalTestVectors = 100;
			GenerateMatlabAndCSV = false;

			IcarusDir = Helpers.Utility.GetFullSystemPath("iverilog.exe");
			if (IcarusDir == null)
			{
#if DEBUG
				IcarusDir = "E:\\iverilog\\bin\\iverilog.exe";
#else
				IcarusDir = "Enter Icarus iVerilog \'bin\' directory";
#endif
			}
		}

		public bool Select_Fixed2FP { get; set; }
		public bool Select_FP2Fixed { get; set; }

		string _IcarusDir;
		public string IcarusDir
		{
			get { return _IcarusDir; }
			set { _IcarusDir = value; RaisePropertyChanged("IcarusDir"); Found_Icarus = !string.IsNullOrEmpty(value) && File.Exists(value); }
		}
		public int Fx2Fp_TotalBits { get; set; }
		public int Fp2Fx_TotalBits { get; set; }
		public int Fx2Fp_TotalTestVectors { get; set; }
		public int Fp2Fx_TotalTestVectors { get; set; }

		bool _Found_Icarus;
		public bool Found_Icarus
		{
			get { return _Found_Icarus; }
			set { _Found_Icarus = value; RaisePropertyChanged("Found_Icarus"); }
		}

		bool _Executing;
		public bool Executing
		{
			get { return _Executing; }
			set { _Executing = value; RaisePropertyChanged("Executing"); }
		}

		public ICommand ExecuteIcarus { get; private set; }

		string _StatusString;
		public string StatusString
		{
			get { return _StatusString; }
			set { _StatusString = value; RaisePropertyChanged("StatusString"); }
		}

		bool _ShowStatus;
		public bool ShowStatus
		{
			get { return _ShowStatus; }
			set { _ShowStatus = value; RaisePropertyChanged("ShowStatus"); }
		}


		public bool GenerateMatlabAndCSV { get; set; }


	}
}
