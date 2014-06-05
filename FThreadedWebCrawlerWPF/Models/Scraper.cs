using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace FThreadedWebCrawlerWPF.Models
{
	static class Scraper
	{
		public static void Scrape(Uri sourceUri, Uri rootUri, string file, ObservableCollection<Uri> list, List<Uri> doneList)
		{
			// Zoek alle <a> tags
			MatchCollection tags = Regex.Matches(file, @"(<a.*?>.*?</a>)",
				RegexOptions.Singleline);

			foreach (Match m in tags)
			{
				string value = m.Groups[1].Value;
				
				// extract het href target
				Match target = Regex.Match(value, @"href=\""(.*?)\.html\""",
				RegexOptions.Singleline);
				
				if (target.Success)
				{
					string target1 = target.Groups[1].Value + ".html";
					Uri uri;

					if (!target1.StartsWith("http://"))
					{
						string sourceUriString = sourceUri.ToString();
						uri = new Uri(sourceUriString.Substring(0, sourceUriString.LastIndexOf("/") + 1) + target1); //relative uri
					}
					else
						uri = new Uri(target1);
					
					if (!uri.ToString().StartsWith(rootUri.ToString()))
						continue;

					lock (list)
					{
						if (!doneList.Contains(uri) && !list.Contains(uri))
						{
							try
							{
								Application.Current.Dispatcher.Invoke((Action)(() =>
								{
									list.Add(uri);
								}));
							}
							catch (Exception e)
							{

							}
						}
					}
				}
			}
		}
	}
}
