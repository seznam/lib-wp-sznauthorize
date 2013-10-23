using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SznAuthorize.FRPC.Types;
using SznAuthorizeExample.Resources;

namespace SznAuthorizeExample
{
	public partial class MainPage : PhoneApplicationPage
	{
		// Constructor
		public MainPage()
		{
			InitializeComponent();

			// Sample code to localize the ApplicationBar
			//BuildLocalizedApplicationBar();
		}
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			if (SznAuthorize.Connection.UserExist())
			{
				// if we want automatically login user from last session
				SznAuthorize.Connection.TryRefreshSession(this.RefreshSessionResult);
			}
			else
			{
				// we don't have user -> go to login page
				SznAuthorize.Connection.ShowLoginPage(this.NavigationService);
			}
		}

		private void LoginButton_Click(object sender, RoutedEventArgs e)
		{
			//SznAuthorize.ServerProxy..Instance.TryHit();
			SznAuthorize.Connection.ShowLoginPage(this.NavigationService);
		}

		private void LogoutButton_Click(object sender, RoutedEventArgs e)
		{
			//SznAuthorize.ServerProxy..Instance.TryHit();
			SznAuthorize.Connection.Logout();
		}


		private void TestSecureHttpButton_Click(object sender, RoutedEventArgs e)
		{
			if (!SznAuthorize.Connection.SessionExist())
			{
				MessageBox.Show("User session not exit, can't try get user hashId!");
				return;
			}

			// we try get hash for current logged user
			Dictionary<string, string> headers = new Dictionary<string, string>();
			headers.Add("Accept", "application/x-base64-frpc");
			SznAuthorize.AsyncTask task = SznAuthorize.Connection.CreateAuthorizedHttp("http://email.sbeta.cz", headers, null, 3, 0, this.GetHashIdResult);
			SznAuthorize.Connection.EnqueueAsyncTask(task);
		}

		private void TestSecureFrpcButton_Click(object sender, RoutedEventArgs e)
		{
			if (!SznAuthorize.Connection.SessionExist() || m_HashId == string.Empty)
			{
				MessageBox.Show("Session or HashId doesn't exist, can't call authorized frpc!");
				return;
			}

			// create frpc method call
			MethodCall mc = new MethodCall("user.getAttributes");

			// add integer parameter to method call (user id)
			mc.Parameters.Add(new SznAuthorize.FRPC.Types.Integer(SznAuthorize.Connection.UserId));

			// add hashId to http headers
			Dictionary<string, string> headers = new Dictionary<string, string>();
			headers.Add("X-Seznam-hashId", m_HashId);

			// create secure frpc call
			SznAuthorize.AsyncTask task = SznAuthorize.Connection.CreateAuthorizedFRPC("http://email.sbeta.cz/RPC2", headers, mc, 3, 0, this.UserGetAtributesFrpcResult);

			// enqueue async task to queue
			SznAuthorize.Connection.EnqueueAsyncTask(task);
		}

		private void RefreshSessionResult(bool connectionOk, bool refreshOk)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (connectionOk && refreshOk)
				{
					MessageBox.Show("User session correctly refreshed.");
				}
				else
				{
					MessageBox.Show(string.Format("Problem ocurs during session refresh cenectionOk = {0}, refreshOk = {1}", connectionOk, refreshOk));
				}
			});

		}

		private void UserGetAtributesFrpcResult(SznAuthorize.FRPC.Types.BaseType result)
		{
			if (result != null)
			{
				Debug.WriteLine(result.DebugToString());

				// we must go to ui thread
				Deployment.Current.Dispatcher.BeginInvoke(() =>
				{
					MessageBox.Show(result.DebugToString());
				});
			}
		}

		private void GetHashIdResult(WebResponse webResponse, byte[] resultData)
		{
			// result valid data?
			if (webResponse != null && resultData != null)
			{
				// check if response content type is base64-frpc
				if (webResponse.Headers["Content-Type"].Contains("application/x-base64-frpc"))
				{
					// decode base64
					string result = Encoding.UTF8.GetString(resultData, 0, resultData.Length);
					byte[] data = Convert.FromBase64String(result);

					// demarshal frpc
					SznAuthorize.FRPC.Types.BaseType response = SznAuthorize.FRPC.Types.BaseType.FromStream(new BinaryReader(new MemoryStream(data)));

					// show result in debug window
					Debug.WriteLine(response.DebugToString());

					// get hash id from response
					m_HashId = response.GetAsMethodResponse().GetData().GetAsStruct().GetMember("hashId");

					// we must go to ui thread
					Deployment.Current.Dispatcher.BeginInvoke(() =>
					{
						MessageBox.Show(response.DebugToString());
					});

				}
				else
				{
					m_HashId = string.Empty;
				}
			}
		}

		private string m_HashId;
	}
}