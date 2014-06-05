using FThreadedWebCrawlerWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FThreadedWebCrawlerWPF.ViewModels
{
	public class CrawlerListBoxItem : PropertyChangedBase
	{
		private string _name;

		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				NotifyPropertyChanged("Name");
			}
		}

		private double _progress;
		public double Progress 
		{ 
			get { return _progress; } 
			set
			{
				_progress = value;
				NotifyPropertyChanged("Progress");
			}
		}

	}
}
