using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace SznAuthorizeExample
{
	public partial class UserLoginPage : PhoneApplicationPage
	{
		public UserLoginPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			LoginControl.LoadCurrentUser();
		}

		private void LoginControl_LoginDone(object sender, EventArgs e)
		{
			NavigationService.GoBack();
		}

		private void LoginControl_LoginCanceled(object sender, EventArgs e)
		{
			NavigationService.GoBack();
		}
	}
}