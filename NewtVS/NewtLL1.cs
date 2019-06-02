using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace Grimoire
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("Newt", "A rich parser generator in a small package", "1.0")]
	[Guid("47CCEAB8-41C4-4145-B9C2-1D414CC24ADE")]
	[ComVisible(true)]
	[ProvideObject(typeof(NewtLL1))]
	[CodeGeneratorRegistration(typeof(NewtLL1), "NewtLL1", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
	public sealed class NewtLL1 : IVsSingleFileGenerator
	{

		#region IVsSingleFileGenerator Members

		public int DefaultExtension(out string pbstrDefaultExtension)
		{
			pbstrDefaultExtension = ".cs";
			return pbstrDefaultExtension.Length;
		}

		public int Generate(string wszInputFilePath, string bstrInputFileContents,
		  string wszDefaultNamespace, IntPtr[] rgbOutputFileContents,
		  out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
		{
			try
			{
				using (var stm = new MemoryStream())
				{
					EbnfDocument doc = null;
					var msgs = new List<object>();
					var sw = new StreamWriter(stm);

					using (var sr = new StreamReader(wszInputFilePath))
					{
						try
						{
							doc = EbnfDocument.ReadFrom(sr);
						}
						catch (ExpectingException ex)
						{
							var em = string.Concat("Error parsing grammar: ", ex.Message);
							msgs.Add(em);
							WriteHeader(sw, wszInputFilePath, msgs);
							goto done;
						}
					}
					var hasErrors = false;
					foreach (var m in doc.Prepare(false))
					{
						msgs.Add(m);
						if (EbnfErrorLevel.Error == m.ErrorLevel)
							hasErrors = true;
					}

					if (hasErrors)
					{
						// make sure to dump the messages
						WriteHeader(sw, wszInputFilePath, msgs);
						goto done;
					}

					var name = string.Concat(Path.GetFileNameWithoutExtension(wszInputFilePath),"Parser");
					var cfg = doc.ToCfg();
					foreach (var m in cfg.PrepareLL1(false))
					{
						msgs.Add(m);
						if (CfgErrorLevel.Error == m.ErrorLevel)
							hasErrors = true;
					}
					if (hasErrors)
					{
						WriteHeader(sw, wszInputFilePath, msgs);
						goto done;
					}
					var lexer = doc.ToLexer(cfg);
					WriteHeader(sw, wszInputFilePath, msgs);
					var hasNS = !string.IsNullOrEmpty(wszDefaultNamespace);
					if (hasNS)
						sw.WriteLine(string.Concat("namespace ", wszDefaultNamespace, " {"));

					cfg.WriteCSharpTableDrivenLL1ParserClassTo(sw, name, null, lexer);
					if (hasNS)
						sw.WriteLine("}");

					done:

					sw.Flush();
					int length = (int)stm.Length;
					rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
					Marshal.Copy(stm.GetBuffer(), 0, rgbOutputFileContents[0], length);
					pcbOutput = (uint)length;
	

				}
			}
			catch (Exception ex)
			{
				string s = string.Concat("/* ", ex.Message, " */");
				byte[] b = Encoding.UTF8.GetBytes(s);
				int length = b.Length;
				rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
				Marshal.Copy(b, 0, rgbOutputFileContents[0], length);
				pcbOutput = (uint)length;
			}
			return VSConstants.S_OK;
		}
		public static void WriteHeader(TextWriter writer, string wszInputFilePath, IEnumerable<object> msgs)
		{
			if (null != wszInputFilePath)
				writer.WriteLine(string.Concat("#line 1 \"", Path.GetFullPath(wszInputFilePath).Replace("\"", "\"\""), "\""));
			foreach (var m in msgs)
			{
				var em = m as EbnfMessage;
				if (null != em)
				{
					if (EbnfErrorLevel.Warning == em.ErrorLevel)
						writer.Write("#warning ");
					else if (EbnfErrorLevel.Error == em.ErrorLevel)
						writer.Write("#error ");
					else if (EbnfErrorLevel.Message == em.ErrorLevel)
						writer.Write("// Generator ");
					else
						continue;
					writer.WriteLine(em.ToString());
				}
				else
				{
					var cm = m as CfgMessage;
					if (null != cm)
					{
						if (CfgErrorLevel.Warning == cm.ErrorLevel)
							writer.Write("#warning ");
						else if (CfgErrorLevel.Error == cm.ErrorLevel)
							writer.Write("#error ");
						else if (CfgErrorLevel.Message == cm.ErrorLevel)
							writer.Write("// Generator ");
						else
							continue;
						writer.WriteLine(cm.ToString());
					}
					else
						writer.WriteLine(string.Concat("#error ", m));
				}
			}
		}
		#endregion
	}
}
