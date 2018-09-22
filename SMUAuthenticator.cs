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
	public class SMUAuthenticator : Page, IComponentConnector
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

		internal MenuItem SMURole;

		internal MenuItem SMUStudent;

		internal MenuItem SMUStaff;

		internal TextBox Username;

		internal PasswordBox Password;

		internal Button AuthButton;

		private bool _contentLoaded;

		private CookieContainer Lawnet_Cookies
		{
			get;
			set;
		}

		public SMUAuthenticator()
		{
			this.InitializeComponent();
			if (Settings.Default.SMURole == "SMUSTU\\")
			{
				this.SMUStudent_Click(null, new RoutedEventArgs());
			}
			else if (Settings.Default.SMURole == "SMUSTF\\")
			{
				this.SMUStaff_Click(null, new RoutedEventArgs());
			}
			this.Username.Text = ExtensionMethods.Decrypt(Settings.Default.SMUUsername, "");
			this.Password.Password = ExtensionMethods.Decrypt(Settings.Default.SMUPassword, "");
			this.RememberMe.IsChecked = new bool?(Settings.Default.SMURememberMe);
		}

		private bool SMURememberMeState()
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
			if (this.role.Contains("SMU"))
			{
				user_id = this.role + user_id;
				HtmlWeb auth_web = new HtmlWeb();
				HtmlDocument auth_doc = null;
				try
				{
					auth_doc = auth_web.Load("https://login.libproxy.smu.edu.sg/login?auth=shibboleth&url=https://www.lawnet.sg/lawnet/web/lawnet/ip-access");
				}
				catch (Exception)
				{
					System.ValueTuple<string, string, string, CookieContainer> result = new System.ValueTuple<string, string, string, CookieContainer>("FAIL", "", "INTERNET CONNECTION ERROR", null);
					return result;
				}
				if (auth_doc.DocumentNode.SelectSingleNode("//input[@name='SAMLRequest']") == null)
				{
					return new System.ValueTuple<string, string, string, CookieContainer>("AUTHENTICATED", "", "ALREADY AUTHENTICATATED", null);
				}
				string SAMLRequest = auth_doc.DocumentNode.SelectSingleNode("//input[@name='SAMLRequest']").GetAttributeValue("value", "");
				string RelayState = auth_doc.DocumentNode.SelectSingleNode("//input[@name='RelayState']").GetAttributeValue("value", "");
				RestClient arg_11D_0 = new RestClient("https://login.smu.edu.sg/adfs/ls/");
				RestRequest auth_request = new RestRequest(Method.POST);
				auth_request.AddParameter("SAMLRequest", SAMLRequest);
				auth_request.AddParameter("RelayState", RelayState);
				auth_request.AddParameter("UserName", user_id);
				auth_request.AddParameter("Password", pwd);
				auth_request.AddParameter("AuthMethod", "FormsAuthentication");
				IRestResponse auth_response = arg_11D_0.Execute(auth_request);
				HtmlDocument SAMLResponse_html = new HtmlDocument();
				SAMLResponse_html.LoadHtml(auth_response.Content);
				if (SAMLResponse_html.DocumentNode.SelectSingleNode("//input[@name='SAMLResponse']") == null)
				{
					return new System.ValueTuple<string, string, string, CookieContainer>("FAIL", "", "CREDENTIAL ERROR", null);
				}
				string SAMLResponse = SAMLResponse_html.DocumentNode.SelectSingleNode("//input[@name='SAMLResponse']").GetAttributeValue("value", "");
				string RelayStatePost = SAMLResponse_html.DocumentNode.SelectSingleNode("//input[@name='RelayState']").GetAttributeValue("value", "");
				RestClient cookie_client = new RestClient("https://login.libproxy.smu.edu.sg/Shibboleth.sso/SAML2/POST")
				{
					CookieContainer = new CookieContainer()
				};
				RestRequest cookie_request = new RestRequest(Method.POST);
				cookie_request.AddParameter("SAMLResponse", SAMLResponse);
				cookie_request.AddParameter("RelayState", RelayStatePost);
				IRestResponse cookie_response = cookie_client.Execute(cookie_request);
				if (cookie_response.ResponseUri.ToString() == "https://www-lawnet-sg.libproxy.smu.edu.sg/lawnet/group/lawnet/legal-research/basic-search")
				{
					return new System.ValueTuple<string, string, string, CookieContainer>("SUCCESS", cookie_response.Cookies.First<RestResponseCookie>().Name, cookie_response.Cookies.First<RestResponseCookie>().Value, cookie_client.CookieContainer);
				}
				return new System.ValueTuple<string, string, string, CookieContainer>("FAIL", "", "LAWNET UNDER MAINTENANCE", null);
			}
			return new System.ValueTuple<string, string, string, CookieContainer>("FAIL", "", "INVALID SMU ROLE", null);
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
			Random RND = new Random();
			string user_id;
			string pwd;
			else
			{
				user_id = this.Username.Text;
				pwd = this.Password.Password;
			}
			System.ValueTuple<string, string, string, CookieContainer> expr_111 = this.Authenticate(user_id, pwd);
			string result = expr_111.Item1;
			string value = expr_111.Item3;
			CookieContainer cookies = expr_111.Item4;
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
				this.Username.Text.Replace("@law.smu.edu.sg", "").Replace("@jd.smu.edu.sg", ""),
				"!\n\nCookie ID: ",
				value,
				"\nSession ID: ",
				session_id
			}));
			if (this.SMURememberMeState())
			{
				Settings.Default.SMUUsername = ExtensionMethods.Encrypt(this.Username.Text.Replace("@law.smu.edu.sg", "").Replace("@jd.smu.edu.sg", ""), "");
				Settings.Default.SMUPassword = ExtensionMethods.Encrypt(this.Password.Password, "");
				Settings.Default.SMURole = this.role;
				Settings.Default.SMURememberMe = true;
				Settings.Default.Save();
			}
			else
			{
				Settings.Default.SMUUsername = "";
				Settings.Default.SMUPassword = "";
				Settings.Default.SMURole = "";
				Settings.Default.SMURememberMe = false;
				Settings.Default.Save();
			}
			base.NavigationService.Navigate(new SMUDownloader(this.Lawnet_Cookies, user_id.Replace("@law.smu.edu.sg", "").Replace("@jd.smu.edu.sg", ""), session_id));
		}

		private void About_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("LawNet Reading List Downloader (LRLD) " + Assembly.GetEntryAssembly().GetName().Version.ToString() + " (Windows Store version)\n\nThe LRLD is distributed under the GNU General Public License Terms and is maintained by the Singapore Management University's Legal Innovation and Technology (LIT) Co-curricular Activity.\n\nThe cases and materials downloaded using the LRLD come from the Singapore Academy of Law's LawNet service and are subject to their Terms and Conditions which can be found on the LawNet website.\n\n© 2018 Wan Ding Yao, Ng Jun Xuan, and Gabriel Tan.");
		}

		private void NUS_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.School = "NUS";
			Settings.Default.Save();
			base.NavigationService.Navigate(new NUSAuthenticator());
		}

		private void SMU_Click(object sender, RoutedEventArgs e)
		{
			this.SchoolStatus.Header = "_SMU - Change School ⬇";
			Settings.Default.School = "SMU";
			Settings.Default.Save();
		}

		private void SMUStudent_Click(object sender, RoutedEventArgs e)
		{
			this.SMURole.Header = "_Student";
			if (this.role != "SMUSTU\\")
			{
				this.Username.Text = "";
				this.Password.Password = "";
			}
			this.role = "SMUSTU\\";
			this.Username.IsEnabled = true;
			this.Password.IsEnabled = true;
			this.Username.Tag = "e.g. dingyao.wan.2017";
		}

		private void SMUStaff_Click(object sender, RoutedEventArgs e)
		{
			this.SMURole.Header = "_Staff";
			if (this.role != "SMUSTF\\")
			{
				this.Username.Text = "";
				this.Password.Password = "";
			}
			this.role = "SMUSTF\\";
			this.Username.IsEnabled = true;
			this.Password.IsEnabled = true;
			this.Username.Tag = "e.g. jerroldsoh";
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
			Uri resourceLocater = new Uri("/LRLD;component/smuauthenticator.xaml", UriKind.Relative);
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
				this.SMURole = (MenuItem)target;
				return;
			case 10:
				this.SMUStudent = (MenuItem)target;
				this.SMUStudent.Click += new RoutedEventHandler(this.SMUStudent_Click);
				return;
			case 11:
				this.SMUStaff = (MenuItem)target;
				this.SMUStaff.Click += new RoutedEventHandler(this.SMUStaff_Click);
				return;
			case 12:
				this.Username = (TextBox)target;
				return;
			case 13:
				this.Password = (PasswordBox)target;
				return;
			case 14:
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
