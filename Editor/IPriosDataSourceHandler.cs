using System.Collections.Generic;
using System.Threading.Tasks;

namespace PriosTools
{
	public interface IPriosDataSourceHandler
	{
		string SourceType { get; }

		bool CanHandle(string url);
		Task<List<PriosDataStore.RawDataEntry>> FetchDataAsync(string url);
		void OpenInBrowser(string url);
	}

}
