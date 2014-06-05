using FThreadedWebCrawlerWPF.Models;
using FThreadedWebCrawlerWPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FThreadedWebCrawlerWPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainVM mainVM;

		public MainWindow()
		{
			InitializeComponent();
			
			this.mainVM = new MainVM();
			
			this.DataContext = mainVM;
			lbTodoList.DataContext = mainVM.TodoItems;
			lbUris.DataContext = mainVM.UriItems;

		}

		public void crawlFinished()
		{
			mainVM.ResumeEnabled = false;
			mainVM.PauseEnabled = false;
			mainVM.CrawlEnabled = true;
		}

		private void submit_Click(object sender, RoutedEventArgs e)
		{
			mainVM.ResumeEnabled = false;
			mainVM.PauseEnabled = true;
			mainVM.CrawlEnabled = false;
			
			mainVM.StartCrawling();
		}

		private void pause_Click(object sender, RoutedEventArgs e)
		{
			mainVM.ResumeEnabled = true;
			mainVM.PauseEnabled = false;

			mainVM.PauseAll();
		}

		private void resume_Click(object sender, RoutedEventArgs e)
		{
			mainVM.ResumeEnabled = false;
			mainVM.PauseEnabled = true;
			mainVM.ResumeAll();
		}

		private void FThreadedWebCrawler_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			mainVM.ResumeAll();
			mainVM.Kill();
		}
	}
}
