using FThreadedWebCrawlerWPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace FThreadedWebCrawlerWPF.ViewModels
{
	
	public class MainVM : PropertyChangedBase
	{
		public const int THREADPOOL_SIZE = 20;

		private bool _terminate = false;

		private ListBox listBoxTodo;

		private ManualResetEvent _re;

		private Thread _startCrawlerThread;

		private bool _UriItemsPopulated;

		private Semaphore _semaphore;

		private string _urlString = "http://docs.oracle.com/javase/tutorial/reallybigindex.html";
		public string UrlString 
		{ 
			get { return _urlString; } 
			set { _urlString = value; NotifyPropertyChanged("UrlString"); }
		}

		private Uri _rootUri;

		public ObservableCollection<CrawlerListBoxItem> TodoItems = new ObservableCollection<CrawlerListBoxItem>();
		public ObservableCollection<Uri> UriItems = new ObservableCollection<Uri>();
		public List<Uri> doneList = new List<Uri>();
		

		private bool _resumeEnabled = false;
		public bool ResumeEnabled
		{
			get { return _resumeEnabled; }
			set	{ _resumeEnabled = value; NotifyPropertyChanged("ResumeEnabled"); }
		}

		private bool _pauseEnabled = false;
		public bool PauseEnabled
		{
			get { return _pauseEnabled; }
			set { _pauseEnabled = value; NotifyPropertyChanged("PauseEnabled"); }
		}

		private bool _crawlEnabled = true;
		public bool CrawlEnabled
		{
			get	{ return _crawlEnabled;	}
			set	{ _crawlEnabled = value; NotifyPropertyChanged("CrawlEnabled");	}
		}

		public MainVM()
		{
			_re = new ManualResetEvent(true);
			_semaphore = new Semaphore(1, THREADPOOL_SIZE); // Wait for first crawler thread to populate UriItems
			_UriItemsPopulated = false;
		}

		public void StartCrawling()
		{
			Console.WriteLine("StartCrawling()");

			UriItems.Clear();
			TodoItems.Clear();
			doneList.Clear();

			if (UrlString.EndsWith(".html"))
				_rootUri = new Uri(UrlString.Substring(0, UrlString.LastIndexOf("/") + 1));
			else
			{
				_rootUri = new Uri(UrlString);
				UrlString += (UrlString.EndsWith("/") ? "index.html" : "/index.html");
			}

			UriItems.Add(new Uri(UrlString));

			_startCrawlerThread = new Thread(new ThreadStart(StartCrawlWorkers)); // CrawlWorker starter thread
			_startCrawlerThread.Start();
		}

		public void StartCrawlWorkers()
		{
			Console.WriteLine("StartCrawlWorkers()");
			//Runs on seperate thread
			while (true)
			{
				_semaphore.WaitOne();
				
				if (_terminate)
				{
					Console.WriteLine("StartCrawlWorkers() Caught terminate signal");
					break;
				}
					

				lock(UriItems)
				{
					if (UriItems.Count > 0)
						new Thread(new ThreadStart(CrawlWorkerMethod)).Start(); // CrawlWorker starter thread
					else
					{
						Console.WriteLine("Ran out of Uri's!");
						break;
					}
						
				}
			}
			ResumeEnabled = false;
			PauseEnabled = false;
			CrawlEnabled = true;
		}

		public void CrawlWorkerMethod()
		{
			Uri currentUri;
			CrawlerListBoxItem item = new CrawlerListBoxItem();
			lock (UriItems)
			{
				if (UriItems.Count <= 0)
				{
					_semaphore.Release();
					Console.WriteLine("CrawlWorker ran out of Uri's!");
					return;
				}

				currentUri = UriItems[0];
				
				item.Name = currentUri.AbsolutePath;

				try
				{
					Application.Current.Dispatcher.Invoke((Action)(() =>
					{
						lock (TodoItems)
						{
							TodoItems.Add(item);
							listBoxTodo.ScrollIntoView(item);
							UriItems.RemoveAt(0);
						}
					}));
				}
				catch (Exception e)
				{

				}
			}
			
			string destinationString = _rootUri.MakeRelativeUri(currentUri).ToString();

			DownloadFile(currentUri, destinationString, item);

			if (_terminate)
			{
				Console.WriteLine("CrawlWorkerMethod() Caught terminate signal");
				_semaphore.Release();
				return;
			}

			if (_UriItemsPopulated == false) //first crawl, UriItems populated (hopefully)
			{
				_UriItemsPopulated = true;
				_semaphore.Release(THREADPOOL_SIZE);
			} else
				_semaphore.Release();
		}

		public void PauseAll() { _re.Reset(); }
		public void ResumeAll() { _re.Set(); }


		private void DownloadFile(Uri sourceUri, string destinationString, CrawlerListBoxItem item)
		{
			try
			{
				long iFileSize = 0;
				int iBufferSize = 1;
				iBufferSize *= 1000;

				int lastSlash = destinationString.LastIndexOf("/") + 1;
				string relativeDirectoryString = destinationString.Substring(0, lastSlash);

				if (relativeDirectoryString.Length > 0)
					Directory.CreateDirectory(relativeDirectoryString);

				FileStream saveFileStream;
				saveFileStream = new FileStream(destinationString,
					FileMode.Create, FileAccess.Write,
					FileShare.ReadWrite);

				System.Net.HttpWebRequest hwRq;
				System.Net.HttpWebResponse hwRes;
				hwRq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(sourceUri);

				//Console.WriteLine(sourceUri);

				Stream smRespStream;
				hwRes = (System.Net.HttpWebResponse)hwRq.GetResponse();
				smRespStream = hwRes.GetResponseStream();

				iFileSize = hwRes.ContentLength;

				int iByteSize;
				int iDownloadedByteSize = 0;
				byte[] downBuffer = new byte[iBufferSize];

				while ((iByteSize = smRespStream.Read(downBuffer, 0, downBuffer.Length)) > 0)
				{
					saveFileStream.Write(downBuffer, 0, iByteSize);
					iDownloadedByteSize += iByteSize;
				
					item.Progress = ((0.0 + iDownloadedByteSize) / iFileSize);

					_re.WaitOne();

					if (_terminate)
					{
						Console.WriteLine("DownloadFile() Caught terminate signal");
						saveFileStream.Close();
						return;
					}
					

					//Thread.Sleep(50); // Debugging, slow things down
				}
				saveFileStream.Close();
			
				item.Name = item.Name + " | Done";

				doneList.Add(sourceUri);

				Scraper.Scrape(sourceUri, _rootUri, File.ReadAllText(destinationString), UriItems, doneList);
			}
			catch (Exception e)
			{
				Console.WriteLine("DownloadFile() Caught Exception: " + e.ToString());
			}
		}

		public void setListBoxTodo(ListBox lb)
		{
			listBoxTodo = lb;
		}

		public void Kill()
		{
			_terminate = true;
		}
	}
}
