﻿using System;
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
				this.NavigationService.Navigate(new Uri("/UserLoginPage.xaml", UriKind.Relative));
			}
		}

		private void LoginButton_Click(object sender, RoutedEventArgs e)
		{
			//SznAuthorize.ServerProxy..Instance.TryHit();
			this.NavigationService.Navigate(new Uri("/UserLoginPage.xaml", UriKind.Relative));
		}

		private void LogoutButton_Click(object sender, RoutedEventArgs e)
		{
			//SznAuthorize.ServerProxy..Instance.TryHit();
			SznAuthorize.Connection.Logout();
		}

		private void GetUserAttributes_Click(object sender, RoutedEventArgs e)
		{
			//SznAuthorize.ServerProxy..Instance.TryHit();
			SznAuthorize.Connection.GetUserAttributes(GetUserAttributesResult);
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

		private void RefreshSessionResult(bool connectionOk, SznAuthorize.SesionStatus sesionStatus)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (connectionOk && sesionStatus == SznAuthorize.SesionStatus.Ok)
				{
					MessageBox.Show("User session correctly refreshed.");
				}
				else
				{
					MessageBox.Show(string.Format("Problem ocurs during session refresh cenectionOk = {0}, sesionStatus = {1}", connectionOk, sesionStatus));
				}
			});

		}

		private void GetUserAttributesResult(bool connectionOk, SznAuthorize.UserAttributes userAttributes)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (connectionOk)
				{
					string result = string.Format(
						"Enabled = {0}\n"+
						"UserName = {1}\n"+
						"Domain = {2}\n"+
						"UserId = {3}\n"+
						"FirstName = {4}\n"+
						"LastName = {5}\n"+
						"Sex = {6}\n"+
						"Language = {7}\n"+
						"Greeting = {8}\n"+
						"Icon = {9}\n"+
						"City = {10}\n"+
						"Country = {11}\n"+
						"CreateDate = {12}\n",
						userAttributes.Enabled,
						userAttributes.UserName,
						userAttributes.Domain,
						userAttributes.UserId,
						userAttributes.FirstName,
						userAttributes.LastName,
						userAttributes.Sex,
						userAttributes.Language,
						userAttributes.Greeting,
						userAttributes.Icon,
						userAttributes.City,
						userAttributes.Country,
						userAttributes.CreateDate);

					MessageBox.Show(result);
				}
				else
				{
					MessageBox.Show(string.Format("Problem ocurs during GetUserAttributes."));
				}
			});

		}

		private void UserGetAtributesFrpcResult(SznAuthorize.FRPC.Types.BaseType result)
		{
			if (result != null)
			{
#if DEBUG
				Debug.WriteLine(result.DebugToString());

				// we must go to ui thread
				Deployment.Current.Dispatcher.BeginInvoke(() =>
				{
					MessageBox.Show(result.DebugToString());
				});
#endif
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

					// get hash id from response
					m_HashId = response.GetAsMethodResponse().GetData().GetAsStruct().GetMember("hashId");

					// show result in debug window
#if DEBUG
					Debug.WriteLine(response.DebugToString());

					// we must go to ui thread
					Deployment.Current.Dispatcher.BeginInvoke(() =>
					{
						MessageBox.Show(response.DebugToString());
					});
#endif

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