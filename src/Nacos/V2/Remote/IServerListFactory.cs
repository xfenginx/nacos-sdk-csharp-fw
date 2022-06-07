namespace Nacos.V2.Remote
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Nacos.V2.Exceptions;
    using Nacos.V2.Utils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IServerListFactory
    {
        string GenNextServer();

        string GetCurrentServer();

        List<string> GetServerList();

        string GetName();

        string GetNamespace();
    }

    public class DefaultServerListManager : IServerListFactory, IDisposable
    {
        private const string FIXED_NAME = "fixed";
        private const string CUSTOM_NAME = "custom";
        private const int DEFAULT_TIMEOUT = 5000;

        private readonly ILogger _logger;

        private readonly NacosSdkOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _contentPath;
        private readonly string _nodesPath;

        private long _refreshServerListInternal = 30000;

        private int _currentIndex = 0;

        private List<string> _serversFromEndpoint = new();

        private List<string> _serverList = new();

        private Timer _refreshServerListTimer;

        private string _name = "";
        private string _addressServerUrl;

        private long _lastServerListRefreshTime = 0L;

        private readonly string _namespace;

        public DefaultServerListManager(ILogger logger, IOptions<NacosSdkOptions> optionsAccs, IHttpClientFactory httpClientFactory = null)
        {
            this._logger = logger;
            this._options = optionsAccs.Value;
            this._httpClientFactory = httpClientFactory;

            this._namespace = _options.Namespace;
            this._contentPath = _options.ContextPath ?? "nacos";
            this._nodesPath = _options.NodesPath ?? "serverlist";

            InitServerAddr();
        }

        private void InitServerAddr()
        {
            if (_options.ServerAddresses != null && _options.ServerAddresses.Any())
            {
                foreach (var item in _options.ServerAddresses)
                {
                    // here only trust the input server addresses of user
                    _serverList.Add(item.TrimEnd('/'));
                }

                _name = _namespace.IsNullOrWhiteSpace()
                    ? $"{FIXED_NAME}-{GetFixedNameSuffix(_serverList)}"
                    : $"{FIXED_NAME}-{GetFixedNameSuffix(_serverList)}-{_namespace}";
            }
            else
            {
                if (_options.EndPoint.IsNullOrWhiteSpace())
                    throw new NacosException(NacosException.CLIENT_INVALID_PARAM, "endpoint is blank");

                _name = _namespace.IsNullOrWhiteSpace()
                   ? $"{CUSTOM_NAME}-{_options.EndPoint}"
                   : $"{CUSTOM_NAME}-{_options.EndPoint}-{_namespace}";

                _addressServerUrl = $"{_options.EndPoint}/{_contentPath}/{_nodesPath}?namespace={_namespace}";

                this._serversFromEndpoint = GetServerListFromEndpoint()
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                this._refreshServerListTimer = new Timer(
                   async x =>
                   {
                       await RefreshSrvIfNeedAsync().ConfigureAwait(false);
                   }, null, 0, _refreshServerListInternal);
            }
        }

        private string GetFixedNameSuffix(List<string> serverIps)
        {
            StringBuilder sb = new(1024);
            string split = "";

            foreach (var item in serverIps)
            {
                sb.Append(split);
                var ip = Regex.Replace(item, "http(s)?://", "");
                sb.Append(ip.Replace(':', '_'));
                split = "-";
            }

            return sb.ToString();
        }

        private async Task<List<string>> GetServerListFromEndpoint()
        {
            var list = new List<string>();
            try
            {
                var header = Nacos.V2.Naming.Utils.NamingHttpUtil.BuildHeader();

                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(DEFAULT_TIMEOUT));

                var httpClient = _httpClientFactory != null
                    ? _httpClientFactory.CreateClient()
                    : new();

                HttpRequestMessage req = new(HttpMethod.Get, _addressServerUrl);
                foreach (var item in header) req.Headers.TryAddWithoutValidation(item.Key, item.Value);

                var resp = await httpClient.SendAsync(req, cts.Token).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    throw new Exception($"Error while requesting: {_addressServerUrl} . Server returned: {resp.StatusCode}");
                }

                var str = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                // the response content format
                // 10.10.0.1
                // 10.10.0.2:8849
                // http://10.10.0.3:8848
                using StringReader sr = new(str);
                while (true)
                {
                    var line = await sr.ReadLineAsync().ConfigureAwait(false);
                    if (line == null || line.Length <= 0)
                        break;

                    list.Add(line);
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[SERVER-LIST] failed to update server list! env: {0}, url: {1}.", _name, _addressServerUrl);
                return null;
            }
        }

        private async Task RefreshSrvIfNeedAsync()
        {
            try
            {
                if (_serverList != null && _serverList.Count > 0) return;

                _logger?.LogDebug("server list provided by user: {0}", string.Join(",", _serverList));

                if (DateTimeOffset.Now.ToUnixTimeSeconds() - _lastServerListRefreshTime < _refreshServerListInternal) return;

                var list = await GetServerListFromEndpoint().ConfigureAwait(false);

                if (list == null || list.Count <= 0)
                    throw new Exception("Can not acquire Nacos list");

                List<string> newServerAddrList = new();

                foreach (var server in list)
                {
                    if (server.StartsWith(Nacos.V2.Common.Constants.HTTPS, StringComparison.OrdinalIgnoreCase)
                        || server.StartsWith(Nacos.V2.Common.Constants.HTTP, StringComparison.OrdinalIgnoreCase))
                    {
                        newServerAddrList.Add(server);
                    }
                    else
                    {
                        newServerAddrList.Add($"{Nacos.V2.Common.Constants.HTTP}{server}");
                    }
                }

                _serversFromEndpoint = newServerAddrList;
                _lastServerListRefreshTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "failed to update server list");
            }
        }

        public string GenNextServer()
        {
            int index = Interlocked.Increment(ref _currentIndex) % GetServerList().Count;
            return GetServerList()[index];
        }

        public string GetCurrentServer()
            => GetServerList()[_currentIndex % GetServerList().Count];

        public List<string> GetServerList()
            => _serverList == null || !_serverList.Any() ? _serversFromEndpoint : _serverList;

        public void Dispose()
        {
            _refreshServerListTimer?.Dispose();
        }

        public string GetName() => _name;

        public string GetNamespace() => _namespace;
    }
}
