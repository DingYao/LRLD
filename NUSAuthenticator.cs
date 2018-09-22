using HtmlAgilityPack;
using LRLD.Properties;
using RestSharp;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace LRLD
{
	public class NUSAuthenticator : Page, IComponentConnector
	{
		private string role;

		internal Image School;

		internal Image SAL;

		internal CheckBox RememberMe;

		internal Button AboutButton;

		internal Button FeedbackButton;

		internal MenuItem SchoolStatus;

		internal MenuItem SMU;

		internal MenuItem NUS;

		internal MenuItem NUSRole;

		internal MenuItem NUSStudent;

		internal MenuItem NUSStaff;

		internal MenuItem NUSVisitor;

		internal TextBox Username;

		internal PasswordBox Password;

		internal Button AuthButton;

		private bool _contentLoaded;

		private CookieContainer Lawnet_Cookies
		{
			get;
			set;
		}

		public NUSAuthenticator()
		{
			this.InitializeComponent();
			if (Settings.Default.NUSRole == "NUSSTU")
			{
				this.NUSStudent_Click(null, new RoutedEventArgs());
			}
			else if (Settings.Default.NUSRole == "NUSSTF")
			{
				this.NUSStaff_Click(null, new RoutedEventArgs());
			}
			else if (Settings.Default.NUSRole == "NUSEXT")
			{
				this.NUSVisitor_Click(null, new RoutedEventArgs());
			}
			this.Username.Text = ExtensionMethods.Decrypt(Settings.Default.NUSUsername, "");
			this.Password.Password = ExtensionMethods.Decrypt(Settings.Default.NUSPassword, "");
			this.RememberMe.IsChecked = new bool?(Settings.Default.NUSRememberMe);
		}

		private bool NUSRememberMeState()
		{
			bool? isChecked = this.RememberMe.IsChecked;
			bool flag = true;
			return isChecked.GetValueOrDefault() == flag & isChecked.HasValue;
		}

		[return: System.Runtime.CompilerServices.TupleElementNames(new string[]
		{
			"result",
			"name",
			"value",
			"cookies"
		})]
		private System.ValueTuple<string, string, string, CookieContainer> Authenticate(string user_id, string pwd)
		{
			if (this.role.Contains("NUS"))
			{
				HtmlWeb auth_web = new HtmlWeb();
				HtmlDocument auth_doc = null;
				try
				{
					auth_doc = auth_web.Load("https://www-lawnet-sg.lawproxy1.nus.edu.sg/lawnet/group/lawnet/legal-research/basic-search");
				}
				catch (Exception)
				{
					System.ValueTuple<string, string, string, CookieContainer> result = new System.ValueTuple<string, string, string, CookieContainer>("FAIL", "", "INTERNET CONNECTION ERROR", null);
					return result;
				}
				if (auth_doc.DocumentNode.SelectSingleNode("//div[@class='resourcesAccordion']") != null)
				{
					return new System.ValueTuple<string, string, string, CookieContainer>("AUTHENTICATED", "", "ALREADY AUTHENTICATATED", null);
				}
				RestClient arg_BE_0 = new RestClient("https://proxylogin.nus.edu.sg/lawproxy1/public/login_form.asp");
				RestRequest auth_request = new RestRequest(Method.POST);
				auth_request.AddParameter("domain", this.role);
				auth_request.AddParameter("user", user_id);
				auth_request.AddParameter("pass", pwd);
				auth_request.AddParameter("url", "http://www.lawnet.sg/lawnet/ip-access");
				IRestResponse auth_response = arg_BE_0.Execute(auth_request);
				HtmlDocument expr_CA = new HtmlDocument();
				expr_CA.LoadHtml(auth_response.Content);
				HtmlNode Response_check = expr_CA.DocumentNode.SelectSingleNode("//form[@action]");
				if (Response_check == null)
				{
					return new System.ValueTuple<string, string, string, CookieContainer>("FAIL", "", "CREDENTIAL ERROR", null);
				}
				HtmlAttribute expr_113 = Response_check.Attributes["action"];
				RestClient cookie_client = new RestClient((expr_113 != null) ? expr_113.Value : null)
				{
					CookieContainer = new CookieContainer()
				};
				RestRequest cookie_request = new RestRequest(Method.POST);
				IRestResponse cookie_response = cookie_client.Execute(cookie_request);
				if (cookie_response.ResponseUri.ToString() == "https://www-lawnet-sg.lawproxy1.nus.edu.sg/lawnet/group/lawnet/legal-research/basic-search")
				{
					return new System.ValueTuple<string, string, string, CookieContainer>("SUCCESS", cookie_response.Cookies.First<RestResponseCookie>().Name, cookie_response.Cookies.First<RestResponseCookie>().Value, cookie_client.CookieContainer);
				}
				return new System.ValueTuple<string, string, string, CookieContainer>("FAIL", "", "LAWNET UNDER MAINTENANCE", null);
			}
			return new System.ValueTuple<string, string, string, CookieContainer>("FAIL", "", "INVALID NUS ROLE", null);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.AuthButton.Content = "Authenticating...";
			this.AuthButton.Refresh();
			if (this.role == null)
			{
				MessageBox.Show("Error: ROLE NOT SELECTED");
				this.AuthButton.Content = "Authenticate";
				return;
			}
			if (this.Username.Text == "")
			{
				MessageBox.Show("Error: BLANK USERNAME");
				this.AuthButton.Content = "Authenticate";
				return;
			}
			if (this.Password.Password == "")
			{
				MessageBox.Show("Error: BLANK PASSWORD");
				this.AuthButton.Content = "Authenticate";
				return;
			}
			string user_id = this.Username.Text;
			string pwd = this.Password.Password;
			Random RND = new Random();
			System.ValueTuple<string, string, string, CookieContainer> expr_D1 = this.Authenticate(user_id, pwd);
			string result = expr_D1.Item1;
			string value = expr_D1.Item3;
			CookieContainer cookies = expr_D1.Item4;
			if (result == "FAIL")
			{
				MessageBox.Show("Error: " + value);
				this.AuthButton.Content = "Authenticate";
				return;
			}
			this.Lawnet_Cookies = cookies;
			string session_id = RND.Next(1, 999999999).ToString();
			MessageBox.Show(string.Concat(new string[]
			{
				"Success! Welcome ",
				this.Username.Text,
				"!\n\nCookie ID: ",
				value,
				"\nSession ID: ",
				session_id
			}));
			if (this.NUSRememberMeState())
			{
				Settings.Default.NUSUsername = ExtensionMethods.Encrypt(this.Username.Text, "");
				Settings.Default.NUSPassword = ExtensionMethods.Encrypt(this.Password.Password, "");
				Settings.Default.NUSRole = this.role;
				Settings.Default.NUSRememberMe = true;
				Settings.Default.Save();
			}
			else
			{
				Settings.Default.NUSUsername = "";
				Settings.Default.NUSPassword = "";
				Settings.Default.NUSRole = "";
				Settings.Default.NUSRememberMe = false;
				Settings.Default.Save();
			}
			base.NavigationService.Navigate(new NUSDownloader(this.Lawnet_Cookies, user_id, session_id));
		}

		private void About_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("LawNet Reading List Downloader (LRLD) " + Assembly.GetEntryAssembly().GetName().Version.ToString() + " (Windows Store version)\n\nThe LRLD is distributed under the GNU General Public License Terms and is maintained by the Singapore Management University's Legal Innovation and Technology (LIT) Co-curricular Activity.\n\nThe cases and materials downloaded using the LRLD come from the Singapore Academy of Law's LawNet service and are subject to their Terms and Conditions which can be found on the LawNet website.\n\n© 2018 Wan Ding Yao, Ng Jun Xuan, and Gabriel Tan.");
		}

		private void NUS_Click(object sender, RoutedEventArgs e)
		{
			this.SchoolStatus.Header = "_NUS - Change School ⬇";
			Settings.Default.School = "NUS";
			Settings.Default.Save();
		}

		private void SMU_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.School = "SMU";
			Settings.Default.Save();
			base.NavigationService.Navigate(new SMUAuthenticator());
		}

		private void NUSStudent_Click(object sender, RoutedEventArgs e)
		{
			this.NUSRole.Header = "_Student";
			if (this.role != "NUSSTU")
			{
				this.Username.Text = "";
				this.Password.Password = "";
			}
			this.role = "NUSSTU";
			this.Username.IsEnabled = true;
			this.Password.IsEnabled = true;
			this.Username.Tag = "e.g. E0200020";
		}

		private void NUSStaff_Click(object sender, RoutedEventArgs e)
		{
			this.NUSRole.Header = "_Staff";
			if (this.role != "NUSSTF")
			{
				this.Username.Text = "";
				this.Password.Password = "";
			}
			this.role = "NUSSTF";
			this.Username.IsEnabled = true;
			this.Password.IsEnabled = true;
			this.Username.Tag = "e.g. ecettt";
		}

		private void NUSVisitor_Click(object sender, RoutedEventArgs e)
		{
			this.NUSRole.Header = "_Visitor";
			if (this.role != "NUSEXT")
			{
				this.Username.Text = "";
				this.Password.Password = "";
			}
			this.role = "NUSEXT";
			this.Username.IsEnabled = true;
			this.Password.IsEnabled = true;
			this.Username.Tag = "";
		}

		private void Feedback_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://tinyurl.com/LRLDFeedback");
		}

		[GeneratedCode("PresentationBuildTasks", "4.0.0.0"), DebuggerNonUserCode]
		public void InitializeComponent()
		{
			if (this._contentLoaded)
			{
				return;
			}
			this._contentLoaded = true;
			Uri resourceLocater = new Uri("/LRLD;component/nusauthenticator.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}

		[GeneratedCode("PresentationBuildTasks", "4.0.0.0"), EditorBrowsable(EditorBrowsableState.Never), DebuggerNonUserCode]
		void IComponentConnector.Connect(int connectionId, object target)
		{
			switch (connectionId)
			{
			case 1:
				this.School = (Image)target;
				return;
			case 2:
				this.SAL = (Image)target;
				return;
			case 3:
				this.RememberMe = (CheckBox)target;
				return;
			case 4:
				this.AboutButton = (Button)target;
				this.AboutButton.Click += new RoutedEventHandler(this.About_Click);
				return;
			case 5:
				this.FeedbackButton = (Button)target;
				this.FeedbackButton.Click += new RoutedEventHandler(this.Feedback_Click);
				return;
			case 6:
				this.SchoolStatus = (MenuItem)target;
				return;
			case 7:
				this.SMU = (MenuItem)target;
				this.SMU.Click += new RoutedEventHandler(this.SMU_Click);
				return;
			case 8:
				this.NUS = (MenuItem)target;
				this.NUS.Click += new RoutedEventHandler(this.NUS_Click);
				return;
			case 9:
				this.NUSRole = (MenuItem)target;
				return;
			case 10:
				this.NUSStudent = (MenuItem)target;
				this.NUSStudent.Click += new RoutedEventHandler(this.NUSStudent_Click);
				return;
			case 11:
				this.NUSStaff = (MenuItem)target;
				this.NUSStaff.Click += new RoutedEventHandler(this.NUSStaff_Click);
				return;
			case 12:
				this.NUSVisitor = (MenuItem)target;
				this.NUSVisitor.Click += new RoutedEventHandler(this.NUSVisitor_Click);
				return;
			case 13:
				this.Username = (TextBox)target;
				return;
			case 14:
				this.Password = (PasswordBox)target;
				return;
			case 15:
				this.AuthButton = (Button)target;
				this.AuthButton.Click += new RoutedEventHandler(this.Button_Click);
				return;
			default:
				this._contentLoaded = true;
				return;
			}
		}
	}
}
