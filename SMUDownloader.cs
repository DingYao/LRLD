using HtmlAgilityPack;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using LRLD.Properties;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using RestSharp;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Xceed.Words.NET;

namespace LRLD
{
	public class SMUDownloader : Page, IComponentConnector
	{
		[CompilerGenerated]
		[Serializable]
		private sealed class <>c
		{
			public static readonly SMUDownloader.<>c <>9 = new SMUDownloader.<>c();

			public static Func<int, string> <>9__26_7;

			public static Func<int, string> <>9__26_8;

			public static Func<int, string> <>9__26_9;

			internal string <Download>b__26_7(int n)
			{
				return n.ToString();
			}

			internal string <Download>b__26_8(int n)
			{
				return n.ToString();
			}

			internal string <Download>b__26_9(int n)
			{
				return n.ToString();
			}
		}

		private string download_directory = "";

		private string[] rl_files;

		private List<ListBox> MyList = new List<ListBox>();

		private TelemetryClient Telemetry = new TelemetryClient
		{
			InstrumentationKey = ""
		};

		private string Light = "";

		private int PDFCount;

		private int HTMLCount;

		private int CaseNotFoundCount;

		private int DuplicateCount;

		internal ListBox Citation;

		internal ListBox DLS;

		internal Button LoadFileButton;

		internal Button SelectDLDirButton;

		internal Button DownloadButton;

		internal CheckBox PreferHTML;

		internal Button SelectAllButton;

		private bool _contentLoaded;

		private CookieContainer Lawnet_Cookies
		{
			get;
			set;
		}

		public SMUDownloader(CookieContainer AuthReceive, string user_id, string session_id)
		{
			this.InitializeComponent();
			this.MyList.Add(this.Citation);
			this.MyList.Add(this.DLS);
			this.PreferHTML.IsChecked = new bool?(Settings.Default.PreferHTML);
			if (Settings.Default.Date != DateTime.Now.ToShortDateString())
			{
				Settings.Default.DLCount = 0;
				Settings.Default.Date = DateTime.Now.ToShortDateString();
				Settings.Default.Save();
			}
			this.Lawnet_Cookies = AuthReceive;
			this.Telemetry.Context.User.Id = "SMU_" + user_id;
			this.Telemetry.Context.Session.Id = session_id;
			this.Telemetry.Context.Component.Version = Assembly.GetEntryAssembly().GetName().Version.ToString();
			this.Telemetry.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
		}

		private string DuplicateCheck(string citation, int i)
		{
			return Application.Current.Dispatcher.Invoke<string>(delegate
			{
				if (this.Citation.Items.Contains(citation))
				{
					this.DLS.Items.RemoveAt(i);
					this.DLS.Items.Insert(i, "Same as " + citation);
					this.DLS.Refresh();
					return "Duplicate";
				}
				this.Citation.Items.RemoveAt(i);
				this.Citation.Items.Insert(i, citation);
				this.Citation.SelectedItems.Add(this.Citation.Items[i]);
				this.Citation.Refresh();
				return "Unique";
			});
		}

		private string CitationChecker(int i)
		{
			return Application.Current.Dispatcher.Invoke<string>(delegate
			{
				if (this.Citation.SelectedItems.Contains(this.Citation.Items[i]))
				{
					return "Yes";
				}
				return "No";
			});
		}

		private string Pad4PDF(string citation)
		{
			string[] elements = citation.Split(new char[]
			{
				' '
			});
			elements[elements.Length - 1] = elements[elements.Length - 1].PadLeft(4, '0');
			citation = string.Join(" ", elements);
			return citation;
		}

		private bool PreferHTMLState()
		{
			return Application.Current.Dispatcher.Invoke<bool>(delegate
			{
				bool? isChecked = this.PreferHTML.IsChecked;
				bool flag = true;
				return isChecked.GetValueOrDefault() == flag & isChecked.HasValue;
			});
		}

		private bool MainPreferHTMLState()
		{
			bool? isChecked = this.PreferHTML.IsChecked;
			bool flag = true;
			return isChecked.GetValueOrDefault() == flag & isChecked.HasValue;
		}

		private Visual GetDescendantByType(Visual element, Type type)
		{
			if (element == null)
			{
				return null;
			}
			if (element.GetType() == type)
			{
				return element;
			}
			Visual foundElement = null;
			if (element is FrameworkElement)
			{
				(element as FrameworkElement).ApplyTemplate();
			}
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
			{
				Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
				foundElement = this.GetDescendantByType(visual, type);
				if (foundElement != null)
				{
					break;
				}
			}
			return foundElement;
		}

		private void Citation_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			ScrollViewer CitationScrollViewer = this.GetDescendantByType(this.Citation, typeof(ScrollViewer)) as ScrollViewer;
			ScrollViewer expr_37 = this.GetDescendantByType(this.DLS, typeof(ScrollViewer)) as ScrollViewer;
			expr_37.Background = Brushes.White;
			expr_37.ScrollToVerticalOffset(CitationScrollViewer.VerticalOffset);
		}

		private void DLS_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			ScrollViewer arg_37_0 = this.GetDescendantByType(this.Citation, typeof(ScrollViewer)) as ScrollViewer;
			ScrollViewer DLSScrollViewer = this.GetDescendantByType(this.DLS, typeof(ScrollViewer)) as ScrollViewer;
			arg_37_0.Background = Brushes.White;
			arg_37_0.ScrollToVerticalOffset(DLSScrollViewer.VerticalOffset);
		}

		private bool SelectFolder(out string fileName)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog
			{
				IsFolderPicker = true
			};
			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				fileName = dialog.FileName;
				return true;
			}
			fileName = "";
			return false;
		}

		private bool SelectFile(out string[] fileNames)
		{
			OpenFileDialog dialog = new OpenFileDialog
			{
				DefaultExt = ".docx",
				Multiselect = true,
				Filter = "Word Documents (*.docx)|*.docx|Word 97-2003 Documents (*.doc)|*.doc|PDF Documents (*.pdf)|*.pdf|Text Documents (*.txt)|*.txt"
			};
			bool? flag = dialog.ShowDialog();
			bool flag2 = true;
			if (flag.GetValueOrDefault() == flag2 & flag.HasValue)
			{
				fileNames = dialog.FileNames;
				return true;
			}
			fileNames = null;
			return false;
		}

		private void Download_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.PreferHTML = this.MainPreferHTMLState();
			Settings.Default.Save();
			if (this.LoadFileButton.Background != Brushes.Green)
			{
				MessageBox.Show("Error: READING LIST NOT LOADED");
				return;
			}
			if (this.SelectDLDirButton.Background != Brushes.Green)
			{
				MessageBox.Show("Error: DOWNLOAD DIRECTORY NOT SELECTED");
				return;
			}
			this.DownloadButton.Refresh();
			this.Light = "";
			for (int i = 0; i < this.Citation.Items.Count; i++)
			{
				if (this.Citation.SelectedItems.Contains(this.Citation.Items[i]) && (this.DLS.Items[i].ToString().Contains("Ready to Download") || this.DLS.Items[i].ToString().Contains("Error: TRY AGAIN")))
				{
					this.Light = "Green";
				}
			}
			if (this.Citation.Items.Count == 0 || this.Citation.SelectedItems.Count == 0 || !this.DLS.Items.Contains("Ready to Download") || this.Light != "Green")
			{
				MessageBox.Show("Error: NO CASES TO DOWNLOAD");
				return;
			}
			RestClient client = new RestClient("https://www-lawnet-sg.libproxy.smu.edu.sg/lawnet/group/lawnet/legal-research/basic-search")
			{
				CookieContainer = this.Lawnet_Cookies
			};
			if (this.Lawnet_Cookies == null)
			{
				MessageBox.Show("Error: NO COOKIES");
				return;
			}
			RestRequest request = new RestRequest(Method.GET);
			if (client.Execute(request).ResponseUri.ToString() != "https://www-lawnet-sg.libproxy.smu.edu.sg/lawnet/group/lawnet/legal-research/basic-search")
			{
				MessageBox.Show("Error: COOKIES EXPIRED");
				base.NavigationService.Navigate(new SMUAuthenticator());
				return;
			}
			EventTelemetry eventTelemetry = new EventTelemetry("Session Opened");
			eventTelemetry.Properties.Add("Search Count", this.Citation.SelectedItems.Count.ToString());
			this.Telemetry.TrackEvent(eventTelemetry);
			this.Telemetry.Flush();
			this.PDFCount = 0;
			this.HTMLCount = 0;
			this.CaseNotFoundCount = 0;
			this.DuplicateCount = 0;
			int CitationCount = this.Citation.Items.Count;
			new Thread(delegate
			{
				this.Download_Handler(CitationCount);
			}).Start();
			this.DownloadButton.Content = "Downloading...";
			this.DownloadButton.Foreground = Brushes.Black;
			this.DownloadButton.IsEnabled = false;
			this.LoadFileButton.Foreground = Brushes.Black;
			this.LoadFileButton.IsEnabled = false;
			this.SelectDLDirButton.Foreground = Brushes.Black;
			this.SelectDLDirButton.IsEnabled = false;
			this.PreferHTML.IsEnabled = false;
			this.SelectAllButton.IsEnabled = false;
		}

		private void Download_Handler(int CitationCount)
		{
			SMUDownloader.<>c__DisplayClass25_0 <>c__DisplayClass25_ = new SMUDownloader.<>c__DisplayClass25_0();
			<>c__DisplayClass25_.<>4__this = this;
			<>c__DisplayClass25_.CitationCount = CitationCount;
			int i;
			int i2;
			for (i = 0; i < <>c__DisplayClass25_.CitationCount; i = i2 + 1)
			{
				if (this.CitationChecker(i) == "Yes" && (this.DLS.Items[i].ToString() == "Ready to Download" || this.DLS.Items[i].ToString() == "Error: TRY AGAIN"))
				{
					base.Dispatcher.Invoke(delegate
					{
						<>c__DisplayClass25_.<>4__this.DLS.Items.RemoveAt(i);
						<>c__DisplayClass25_.<>4__this.DLS.Items.Insert(i, "In Download Queue");
						<>c__DisplayClass25_.<>4__this.DLS.Refresh();
					});
				}
				i2 = i;
			}
			for (int l = 0; l < <>c__DisplayClass25_.CitationCount; l += 5)
			{
				List<Thread> download_threads = new List<Thread>();
				for (int j = 0; j < 5; j++)
				{
					int k = l + j;
					if (k < <>c__DisplayClass25_.CitationCount - 1)
					{
						if (this.CitationChecker(k) == "Yes" && this.DLS.Items[k].ToString() == "In Download Queue")
						{
							Thread.Sleep(500);
							Thread DL = new Thread(delegate
							{
								<>c__DisplayClass25_.<>4__this.Download(<>c__DisplayClass25_.<>4__this.Citation.Items[k].ToString(), k);
							});
							DL.Start();
							download_threads.Add(DL);
						}
						else if (this.CitationChecker(k) == "No" && this.DLS.Items[k].ToString() == "In Download Queue")
						{
							base.Dispatcher.Invoke(delegate
							{
								<>c__DisplayClass25_.<>4__this.DLS.Items.RemoveAt(k);
								<>c__DisplayClass25_.<>4__this.DLS.Items.Insert(k, "Ready to Download");
								<>c__DisplayClass25_.<>4__this.DLS.Refresh();
							});
						}
					}
					else if (k == <>c__DisplayClass25_.CitationCount - 1)
					{
						try
						{
							Thread.Sleep(1000);
							if (this.CitationChecker(<>c__DisplayClass25_.CitationCount - 1) == "Yes" && this.DLS.Items[<>c__DisplayClass25_.CitationCount - 1].ToString() == "In Download Queue")
							{
								Thread.Sleep(500);
								ThreadStart arg_2A5_0;
								if ((arg_2A5_0 = <>c__DisplayClass25_.<>9__7) == null)
								{
									arg_2A5_0 = (<>c__DisplayClass25_.<>9__7 = new ThreadStart(<>c__DisplayClass25_.<Download_Handler>b__7));
								}
								Thread DL2 = new Thread(arg_2A5_0);
								DL2.Start();
								download_threads.Add(DL2);
							}
							else if (this.CitationChecker(<>c__DisplayClass25_.CitationCount - 1) == "No" && this.DLS.Items[<>c__DisplayClass25_.CitationCount - 1].ToString() == "In Download Queue")
							{
								Dispatcher arg_349_0 = base.Dispatcher;
								Action arg_349_1;
								if ((arg_349_1 = <>c__DisplayClass25_.<>9__3) == null)
								{
									arg_349_1 = (<>c__DisplayClass25_.<>9__3 = new Action(<>c__DisplayClass25_.<Download_Handler>b__3));
								}
								arg_349_0.Invoke(arg_349_1);
							}
						}
						catch (Exception)
						{
							base.Dispatcher.Invoke(delegate
							{
								<>c__DisplayClass25_.<>4__this.DLS.Items.RemoveAt(k);
								<>c__DisplayClass25_.<>4__this.DLS.Items.Insert(k, "Searching LawNet");
								<>c__DisplayClass25_.<>4__this.DLS.Refresh();
							});
							Thread.Sleep(3000);
							base.Dispatcher.Invoke(delegate
							{
								<>c__DisplayClass25_.<>4__this.DLS.Items.RemoveAt(k);
								<>c__DisplayClass25_.<>4__this.DLS.Items.Insert(k, "Case Not Found");
								<>c__DisplayClass25_.<>4__this.DLS.Refresh();
							});
							this.CaseNotFoundCount++;
						}
					}
				}
				using (List<Thread>.Enumerator enumerator = download_threads.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						enumerator.Current.Join();
					}
				}
			}
			MessageBox.Show(string.Format("Download Complete!\n\nPDFs Downloaded: {0}\nHTMLs Downloaded: {1}\n\nCases Not Found: {2}\nDuplicate Cases: {3}\n\nTotal Cases Downloaded: {4}\nTotal Cases Processed: {5}", new object[]
			{
				this.PDFCount,
				this.HTMLCount,
				this.CaseNotFoundCount,
				this.DuplicateCount,
				this.PDFCount + this.HTMLCount,
				this.PDFCount + this.HTMLCount + this.CaseNotFoundCount + this.DuplicateCount
			}));
			base.Dispatcher.Invoke(delegate
			{
				<>c__DisplayClass25_.<>4__this.DownloadButton.IsEnabled = true;
				<>c__DisplayClass25_.<>4__this.DownloadButton.Content = "Download";
				<>c__DisplayClass25_.<>4__this.DownloadButton.Foreground = Brushes.White;
				<>c__DisplayClass25_.<>4__this.LoadFileButton.Foreground = Brushes.White;
				<>c__DisplayClass25_.<>4__this.LoadFileButton.IsEnabled = true;
				<>c__DisplayClass25_.<>4__this.SelectDLDirButton.Foreground = Brushes.White;
				<>c__DisplayClass25_.<>4__this.SelectDLDirButton.IsEnabled = true;
				<>c__DisplayClass25_.<>4__this.PreferHTML.IsEnabled = true;
				<>c__DisplayClass25_.<>4__this.SelectAllButton.IsEnabled = true;
			});
		}

		private void Download(string citation, int i)
		{
			if (this.DLS.Items[i].ToString() == "In Download Queue")
			{
				base.Dispatcher.Invoke(delegate
				{
					this.DLS.Items.RemoveAt(i);
					this.DLS.Items.Insert(i, "Searching LawNet");
					this.DLS.Refresh();
				});
				RestClient client = new RestClient("https://www-lawnet-sg.libproxy.smu.edu.sg/lawnet/group/lawnet/result-page?p_p_id=legalresearchresultpage_WAR_lawnet3legalresearchportlet&p_p_lifecycle=1&p_p_state=normal&p_p_mode=view&p_p_col_id=column-2&p_p_col_count=1&_legalresearchresultpage_WAR_lawnet3legalresearchportlet_action=basicSeachActionURL&_legalresearchresultpage_WAR_lawnet3legalresearchportlet_searchType=0")
				{
					CookieContainer = this.Lawnet_Cookies
				};
				RestRequest search_request = new RestRequest(Method.POST);
				search_request.AddParameter("grouping", "1", ParameterType.QueryString);
				search_request.AddParameter("category", "1,2,4,5,6,7,8,26,27", ParameterType.QueryString);
				search_request.AddParameter("basicSearchKey", citation, ParameterType.QueryString);
				IRestResponse search_response = client.Execute(search_request);
				HtmlDocument expr_B2 = new HtmlDocument();
				expr_B2.LoadHtml(search_response.Content);
				HtmlNodeCollection case_urls = expr_B2.DocumentNode.SelectNodes("//a[@class='document-title']");
				HtmlNode case_first = expr_B2.DocumentNode.SelectSingleNode("//a[@class='document-title']");
				string case_id = "";
				string case_filename = "";
				if (case_urls == null && citation.Contains("AC"))
				{
					citation = citation.Replace("AC", "A.C.");
					search_request.AddOrUpdateParameter("basicSearchKey", citation, ParameterType.QueryString);
					search_response = client.Execute(search_request);
					HtmlDocument expr_133 = new HtmlDocument();
					expr_133.LoadHtml(search_response.Content);
					case_urls = expr_133.DocumentNode.SelectNodes("//a[@class='document-title']");
					case_first = expr_133.DocumentNode.SelectSingleNode("//a[@class='document-title']");
					if (case_urls == null)
					{
						base.Dispatcher.Invoke(delegate
						{
							this.DLS.Items.RemoveAt(i);
							this.DLS.Items.Insert(i, "Case Not Found");
							this.DLS.Refresh();
						});
						this.CaseNotFoundCount++;
						EventTelemetry eventTelemetry = new EventTelemetry("Case Not Found");
						eventTelemetry.Properties.Add("Citation", citation);
						this.Telemetry.TrackEvent(eventTelemetry);
						this.Telemetry.Flush();
						return;
					}
				}
				else if (case_urls == null)
				{
					base.Dispatcher.Invoke(delegate
					{
						this.DLS.Items.RemoveAt(i);
						this.DLS.Items.Insert(i, "Case Not Found");
						this.DLS.Refresh();
					});
					this.CaseNotFoundCount++;
					EventTelemetry eventTelemetry2 = new EventTelemetry("Case Not Found");
					eventTelemetry2.Properties.Add("Citation", citation);
					this.Telemetry.TrackEvent(eventTelemetry2);
					this.Telemetry.Flush();
					return;
				}
				foreach (HtmlNode case_url in ((IEnumerable<HtmlNode>)case_urls))
				{
					if (case_url.InnerText.ToString().Contains(citation.Replace("Ch", "Ch.")))
					{
						case_id = case_url.GetAttributeValue("onclick", "").Substring(17);
						case_id = case_id.Substring(0, case_id.Length - 6);
						if (case_url.InnerText.ToString().Substring(0, 1) == " ")
						{
							case_filename = case_url.InnerText.ToString().Replace("&nbsp;", " ").Replace("A.C.", "AC").Replace("\\", "").Replace("/", "").Replace(":", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "").Substring(1);
						}
						else
						{
							case_filename = case_url.InnerText.ToString().Replace("&nbsp;", " ").Replace("A.C.", "AC").Replace("\\", "").Replace("/", "").Replace(":", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "");
						}
					}
				}
				if (citation.Contains("SGHC") || citation.Contains("SGCA") || citation.Contains("SGSAB"))
				{
					base.Dispatcher.Invoke(delegate
					{
						this.DLS.Items.RemoveAt(i);
						this.DLS.Items.Insert(i, "Searching for SLR / SSAR");
						this.DLS.Refresh();
					});
					client.BaseUrl = new Uri("https://www-lawnet-sg.libproxy.smu.edu.sg/lawnet/group/lawnet/page-content?p_p_id=legalresearchpagecontent_WAR_lawnet3legalresearchportlet&p_p_lifecycle=1&p_p_state=normal&p_p_mode=view&p_p_col_id=column-2&p_p_col_count=1&_legalresearchpagecontent_WAR_lawnet3legalresearchportlet_action=openContentPage");
					RestRequest check_request = new RestRequest(Method.POST);
					string check_id = case_first.GetAttributeValue("onclick", "").Substring(17);
					check_id = check_id.Substring(0, check_id.Length - 6);
					check_request.AddParameter("contentDocID", check_id + ".xml", ParameterType.QueryString);
					IRestResponse check_response = client.Execute(check_request);
					HtmlDocument expr_4AF = new HtmlDocument();
					expr_4AF.LoadHtml(check_response.Content);
					HtmlNode check_check_html = expr_4AF.DocumentNode.SelectSingleNode("//div[@class='titleCitation']");
					HtmlNode check_check_citation = expr_4AF.DocumentNode.SelectSingleNode("//span[@class='Citation offhyperlink']");
					if (check_check_html.InnerText.Contains(citation))
					{
						case_id = check_id;
						if (check_check_html.InnerText.ToString().Substring(0, 1) == " ")
						{
							case_filename = check_check_html.InnerText.ToString().Replace("&nbsp;", " ").Replace("A.C.", "AC").Replace("\\", "").Replace("\"", "").Replace("/", "").Replace(":", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "").Substring(1);
						}
						else
						{
							case_filename = check_check_html.InnerText.ToString().Replace("&nbsp;", " ").Replace("A.C.", "AC").Replace("\\", "").Replace("\"", "").Replace("/", "").Replace(":", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "");
						}
						if (check_check_citation != null && citation != check_check_citation.InnerText.ToString())
						{
							citation = check_check_citation.InnerText.ToString();
							if (this.DuplicateCheck(citation, i) == "Duplicate")
							{
								this.DuplicateCount++;
								return;
							}
						}
					}
				}
				if (case_id.Length == 0)
				{
					base.Dispatcher.Invoke(delegate
					{
						this.DLS.Items.RemoveAt(i);
						this.DLS.Items.Insert(i, "Case Not Found");
						this.DLS.Refresh();
					});
					this.CaseNotFoundCount++;
					EventTelemetry eventTelemetry3 = new EventTelemetry("Case Not Found");
					eventTelemetry3.Properties.Add("Citation", citation);
					this.Telemetry.TrackEvent(eventTelemetry3);
					this.Telemetry.Flush();
					return;
				}
				if ((citation.Contains("SLR") || citation.Contains("Ch") || citation.Contains("Fam") || citation.Contains("AC") || citation.Contains("A.C.") || citation.Contains("WLR") || citation.Contains("QB") || citation.Contains("SSAR")) && !citation.Contains("SSLR") && !citation.Contains("FMSLR") && !this.PreferHTMLState())
				{
					if (Settings.Default.DLCount > 150)
					{
						base.Dispatcher.Invoke(delegate
						{
							this.DLS.Items.RemoveAt(i);
							this.DLS.Items.Insert(i, "Daily Download Limit Exceeded");
							this.DLS.Refresh();
						});
						return;
					}
					base.Dispatcher.Invoke(delegate
					{
						this.DLS.Items.RemoveAt(i);
						this.DLS.Items.Insert(i, "Downloading PDF");
						this.DLS.Refresh();
					});
					client.BaseUrl = new Uri("https://www-lawnet-sg.libproxy.smu.edu.sg/lawnet/group/lawnet/page-content?p_p_id=legalresearchpagecontent_WAR_lawnet3legalresearchportlet&p_p_lifecycle=2&p_p_resource_id=viewPDFSourceDocument");
					RestRequest PDF_request = new RestRequest(Method.POST);
					PDF_request.AddParameter("pdfFileName", citation, ParameterType.QueryString);
					string PDF_resource_name;
					if (citation.Contains("SLR") || citation.Contains("SSAR"))
					{
						if (citation.Contains("SSAR"))
						{
							IEnumerable<int> arg_879_0 = Enumerable.Range(1985, 2010);
							Func<int, string> arg_879_1;
							if ((arg_879_1 = SMUDownloader.<>c.<>9__26_7) == null)
							{
								arg_879_1 = (SMUDownloader.<>c.<>9__26_7 = new Func<int, string>(SMUDownloader.<>c.<>9.<Download>b__26_7));
							}
							if (arg_879_0.Select(arg_879_1).ToList<string>().Any(new Func<string, bool>(citation.Contains)))
							{
								PDF_resource_name = this.Pad4PDF(citation);
								PDF_resource_name = "(1985-2010)" + PDF_resource_name.Substring(6, PDF_resource_name.Length - 6);
								goto IL_A66;
							}
						}
						PDF_resource_name = this.Pad4PDF(citation);
					}
					else
					{
						if (citation.Contains("WLR"))
						{
							IEnumerable<int> arg_90D_0 = Enumerable.Range(2008, 2021);
							Func<int, string> arg_90D_1;
							if ((arg_90D_1 = SMUDownloader.<>c.<>9__26_8) == null)
							{
								arg_90D_1 = (SMUDownloader.<>c.<>9__26_8 = new Func<int, string>(SMUDownloader.<>c.<>9.<Download>b__26_8));
							}
							if (arg_90D_0.Select(arg_90D_1).ToList<string>().Any(new Func<string, bool>(citation.Contains)))
							{
								PDF_resource_name = citation.Replace(" ", "-").Replace("[", "").Replace("]", "");
								goto IL_A66;
							}
						}
						if (citation.Contains("AC") || citation.Contains("QB"))
						{
							IEnumerable<int> arg_9AA_0 = Enumerable.Range(2008, 2021);
							Func<int, string> arg_9AA_1;
							if ((arg_9AA_1 = SMUDownloader.<>c.<>9__26_9) == null)
							{
								arg_9AA_1 = (SMUDownloader.<>c.<>9__26_9 = new Func<int, string>(SMUDownloader.<>c.<>9.<Download>b__26_9));
							}
							if (arg_9AA_0.Select(arg_9AA_1).ToList<string>().Any(new Func<string, bool>(citation.Contains)))
							{
								if (citation.Contains("QB") && !citation.Contains(" 1 QB"))
								{
									PDF_resource_name = citation.Replace(" QB", " 1 QB");
									PDF_resource_name = PDF_resource_name.Replace(" ", "-").Substring(0, 11) + "." + PDF_resource_name.Substring(12);
									goto IL_A66;
								}
								PDF_resource_name = citation.Replace(" ", "-").Substring(0, 11) + "." + citation.Substring(12);
								goto IL_A66;
							}
						}
						PDF_resource_name = citation.Replace("A.C.", "AC");
					}
					IL_A66:
					PDF_request.AddParameter("pdfFileUri", case_id + "/resource/" + PDF_resource_name + ".pdf", ParameterType.QueryString);
					IRestResponse PDF_response = client.Execute(PDF_request);
					try
					{
						File.WriteAllBytes(this.download_directory + "\\" + case_filename + ".pdf", PDF_response.RawBytes);
						base.Dispatcher.Invoke(delegate
						{
							this.DLS.Items.RemoveAt(i);
							this.DLS.Items.Insert(i, "PDF Downloaded");
							this.DLS.Refresh();
							Settings.Default.DLCount++;
							Settings.Default.Save();
						});
						this.PDFCount++;
						EventTelemetry eventTelemetry4 = new EventTelemetry("PDF Downloaded");
						eventTelemetry4.Properties.Add("Citation", citation);
						this.Telemetry.TrackEvent(eventTelemetry4);
						this.Telemetry.Flush();
						return;
					}
					catch (IOException)
					{
						base.Dispatcher.Invoke(delegate
						{
							this.DLS.Items.RemoveAt(i);
							this.DLS.Items.Insert(i, "Error: FILE IN USE");
							this.DLS.Refresh();
							Settings.Default.DLCount++;
							Settings.Default.Save();
						});
						return;
					}
				}
				if (Settings.Default.DLCount > 150)
				{
					base.Dispatcher.Invoke(delegate
					{
						this.DLS.Items.RemoveAt(i);
						this.DLS.Items.Insert(i, "Daily Download Limit Exceeded");
						this.DLS.Refresh();
					});
					return;
				}
				base.Dispatcher.Invoke(delegate
				{
					this.DLS.Items.RemoveAt(i);
					this.DLS.Items.Insert(i, "Downloading HTML");
					this.DLS.Refresh();
				});
				client.BaseUrl = new Uri("https://www-lawnet-sg.libproxy.smu.edu.sg/lawnet/group/lawnet/page-content?p_p_id=legalresearchpagecontent_WAR_lawnet3legalresearchportlet&p_p_lifecycle=1&p_p_state=pop_up&p_p_mode=view&p_p_col_id=column-2&p_p_col_count=1&_legalresearchpagecontent_WAR_lawnet3legalresearchportlet_action=viewDocumentInReadingPane&highlightActive=true");
				RestRequest HTML_request = new RestRequest(Method.POST);
				HTML_request.AddParameter("_legalresearchpagecontent_WAR_lawnet3legalresearchportlet_documentID", case_id + ".xml", ParameterType.QueryString);
				IRestResponse HTML_response = client.Execute(HTML_request);
				try
				{
					File.WriteAllBytes(this.download_directory + "\\" + case_filename + ".html", HTML_response.RawBytes);
					base.Dispatcher.Invoke(delegate
					{
						this.DLS.Items.RemoveAt(i);
						this.DLS.Items.Insert(i, "HTML Downloaded");
						this.DLS.Refresh();
						Settings.Default.DLCount++;
						Settings.Default.Save();
					});
					this.HTMLCount++;
					EventTelemetry eventTelemetry5 = new EventTelemetry("HTML Downloaded");
					eventTelemetry5.Properties.Add("Citation", citation);
					this.Telemetry.TrackEvent(eventTelemetry5);
					this.Telemetry.Flush();
				}
				catch (IOException)
				{
					base.Dispatcher.Invoke(delegate
					{
						this.DLS.Items.RemoveAt(i);
						this.DLS.Items.Insert(i, "Error: FILE IN USE");
						this.DLS.Refresh();
						Settings.Default.DLCount++;
						Settings.Default.Save();
					});
				}
			}
		}

		private void Load_Click(object sender, RoutedEventArgs e)
		{
			bool? flag = new bool?(this.SelectFile(out this.rl_files));
			bool flag2 = true;
			if (flag.GetValueOrDefault() == flag2 & flag.HasValue)
			{
				if (this.rl_files.Length > 1)
				{
					MessageBox.Show("Selected Reading Lists in: " + System.IO.Path.GetDirectoryName(this.rl_files[0]));
				}
				else
				{
					MessageBox.Show("Selected Reading List: " + System.IO.Path.GetFileName(this.rl_files[0]));
				}
				string[] array = this.rl_files;
				for (int m = 0; m < array.Length; m++)
				{
					string rl_file = array[m];
					if (this.SelectDLDirButton.Background == Brushes.Green)
					{
						this.DownloadButton.Background = Brushes.Green;
					}
					if (rl_file.Contains(".docx"))
					{
						DocX reading_list = null;
						try
						{
							reading_list = DocX.Load(rl_file);
						}
						catch (IOException)
						{
							MessageBox.Show("Error: FILE IN USE");
							return;
						}
						string CiteRegex = "[\\[\\(][1-2]\\d{3}(?:\\-[1-2]\\d{3})?[\\]\\)]\\s[\\d\\s]*[LR]+\\s\\d+\\s[EqCP]+\\s+\\d+|[\\[\\(][1-2]\\d{3}(?:\\-[1-2]\\d{3})?[\\]\\)]\\s[\\d\\s]*[SLR()WMJChAFQBtram\\.]+\\s\\d+|\\[[1-2]\\d{3}(?:\\-[1-2]\\d{3})?\\]\\s[A-Za-z()\\.]+\\s\\d+";
						List<string> citationList = reading_list.FindUniqueByPattern(CiteRegex, RegexOptions.IgnoreCase);
						for (int i = 0; i < citationList.Count; i++)
						{
							if (!this.Citation.Items.Contains(citationList[i].Replace(".", "")))
							{
								this.Citation.Items.Add(citationList[i].Replace(".", ""));
								this.DLS.Items.Add("Ready to Download");
							}
						}
					}
					else
					{
						if (rl_file.Contains(".doc"))
						{
							Document reading_list2 = new Document();
							try
							{
								reading_list2.LoadFromFile(rl_file);
							}
							catch (IOException)
							{
								MessageBox.Show("Error: FILE IN USE");
								return;
							}
							Regex CiteRegex2 = new Regex("[\\[\\(][1-2]\\d{3}(?:\\-[1-2]\\d{3})?[\\]\\)]\\s[\\d\\s]*[LR]+\\s\\d+\\s[EqCP]+\\s+\\d+|[\\[\\(][1-2]\\d{3}(?:\\-[1-2]\\d{3})?[\\]\\)]\\s[\\d\\s]*[SLR()WMJChAFQBtram\\.]+\\s\\d+|\\[[1-2]\\d{3}(?:\\-[1-2]\\d{3})?\\]\\s[A-Za-z()\\.]+\\s\\d+");
							TextSelection[] arg_1BE_0 = reading_list2.FindAllPattern(CiteRegex2);
							IList<TextRange> ranges = new List<TextRange>();
							TextSelection[] array2 = arg_1BE_0;
							for (int n = 0; n < array2.Length; n++)
							{
								TextRange range = array2[n].GetAsOneRange();
								ranges.Add(range);
							}
							using (IEnumerator<TextRange> enumerator = ranges.GetEnumerator())
							{
								while (enumerator.MoveNext())
								{
									TextRange range2 = enumerator.Current;
									string text = range2.Text;
									range2.Text = string.Format("{0}", text);
									if (!this.Citation.Items.Contains(range2.Text.Replace(".", "")))
									{
										this.Citation.Items.Add(range2.Text.Replace(".", ""));
										this.DLS.Items.Add("Ready to Download");
									}
								}
								goto IL_4D3;
							}
						}
						if (rl_file.Contains(".pdf"))
						{
							PdfReader reading_list3 = null;
							try
							{
								reading_list3 = new PdfReader(rl_file);
							}
							catch (IOException)
							{
								MessageBox.Show("Error: FILE IN USE");
								return;
							}
							StringBuilder text2 = new StringBuilder();
							for (int j = 1; j <= reading_list3.NumberOfPages; j++)
							{
								text2.Append(PdfTextExtractor.GetTextFromPage(reading_list3, j));
							}
							MatchCollection citationList2 = new Regex("[\\[\\(][1-2]\\d{3}(?:\\-[1-2]\\d{3})?[\\]\\)]\\s[\\d\\s]*[LR]+\\s\\d+\\s[EqCP]+\\s+\\d+|[\\[\\(][1-2]\\d{3}(?:\\-[1-2]\\d{3})?[\\]\\)]\\s[\\d\\s]*[SLR()WMJChAFQBtram\\.]+\\s\\d+|\\[[1-2]\\d{3}(?:\\-[1-2]\\d{3})?\\]\\s[A-Za-z()\\.]+\\s\\d+").Matches(text2.ToString());
							for (int k = 0; k < citationList2.Count; k++)
							{
								if (!this.Citation.Items.Contains(citationList2[k].ToString().Replace('\n'.ToString(), "").Replace(".", "")))
								{
									this.Citation.Items.Add(citationList2[k].ToString().Replace('\n'.ToString(), "").Replace(".", ""));
									this.DLS.Items.Add("Ready to Download");
								}
							}
						}
						else if (rl_file.Contains(".txt"))
						{
							string readContents;
							using (StreamReader streamReader = new StreamReader(rl_file, Encoding.UTF8))
							{
								readContents = streamReader.ReadToEnd();
							}
							MatchCollection citationList3 = new Regex("[\\[\\(][1-2]\\d{3}(?:\\-[1-2]\\d{3})?[\\]\\)]\\s[\\d\\s]*[LR]+\\s\\d+\\s[EqCP]+\\s+\\d+|[\\[\\(][1-2]\\d{3}(?:\\-[1-2]\\d{3})?[\\]\\)]\\s[\\d\\s]*[SLR()WMJChAFQBtram\\.]+\\s\\d+|\\[[1-2]\\d{3}(?:\\-[1-2]\\d{3})?\\]\\s[A-Za-z()\\.]+\\s\\d+").Matches(readContents);
							for (int l = 0; l < citationList3.Count; l++)
							{
								if (!this.Citation.Items.Contains(citationList3[l].ToString().Replace('\n'.ToString(), "").Replace(".", "")))
								{
									this.Citation.Items.Add(citationList3[l].ToString().Replace('\n'.ToString(), "").Replace(".", ""));
									this.DLS.Items.Add("Ready to Download");
								}
							}
						}
					}
					IL_4D3:;
				}
				if (this.Citation.Items.Count > 0)
				{
					this.LoadFileButton.Background = Brushes.Green;
					this.LoadFileButton.Content = "Add Reading List(s)";
				}
			}
		}

		private void Directory_Click(object sender, RoutedEventArgs e)
		{
			bool? flag = new bool?(this.SelectFolder(out this.download_directory));
			bool flag2 = true;
			if (flag.GetValueOrDefault() == flag2 & flag.HasValue)
			{
				MessageBox.Show("Selected Download Directory: " + this.download_directory);
				this.SelectDLDirButton.Background = Brushes.Green;
				this.SelectDLDirButton.Content = "Change Download Directory";
				if (this.LoadFileButton.Background == Brushes.Green)
				{
					this.DownloadButton.Background = Brushes.Green;
				}
			}
		}

		private void SelectAll_Click(object sender, RoutedEventArgs e)
		{
			this.Citation.SelectAll();
		}

		[GeneratedCode("PresentationBuildTasks", "4.0.0.0"), DebuggerNonUserCode]
		public void InitializeComponent()
		{
			if (this._contentLoaded)
			{
				return;
			}
			this._contentLoaded = true;
			Uri resourceLocater = new Uri("/LRLD;component/smudownloader.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}

		[GeneratedCode("PresentationBuildTasks", "4.0.0.0"), EditorBrowsable(EditorBrowsableState.Never), DebuggerNonUserCode]
		void IComponentConnector.Connect(int connectionId, object target)
		{
			switch (connectionId)
			{
			case 1:
				this.Citation = (ListBox)target;
				this.Citation.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(this.Citation_ScrollChanged));
				return;
			case 2:
				this.DLS = (ListBox)target;
				this.DLS.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(this.DLS_ScrollChanged));
				return;
			case 3:
				this.LoadFileButton = (Button)target;
				this.LoadFileButton.Click += new RoutedEventHandler(this.Load_Click);
				return;
			case 4:
				this.SelectDLDirButton = (Button)target;
				this.SelectDLDirButton.Click += new RoutedEventHandler(this.Directory_Click);
				return;
			case 5:
				this.DownloadButton = (Button)target;
				this.DownloadButton.Click += new RoutedEventHandler(this.Download_Click);
				return;
			case 6:
				this.PreferHTML = (CheckBox)target;
				return;
			case 7:
				this.SelectAllButton = (Button)target;
				this.SelectAllButton.Click += new RoutedEventHandler(this.SelectAll_Click);
				return;
			default:
				this._contentLoaded = true;
				return;
			}
		}
	}
}
