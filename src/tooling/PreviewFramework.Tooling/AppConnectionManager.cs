using System.Net.Sockets;
using PreviewFramework.SharedModel.Protocol;
using StreamJsonRpc;

namespace PreviewFramework.Tooling;

public sealed class AppConnectionManager(AppsManager appsManager, TcpClient tcpClient) :
    IPreviewAppControllerService
{
    private readonly AppsManager _appsManager = appsManager;
    private readonly TcpClient _tcpClient = tcpClient;
    private JsonRpc? _rpc;
    private AppManager? _appManager;

    public IPreviewAppService? AppService { get; private set; }

    public string? PlatformName { get; set; }
    public UIComponentsManagerTooling? UIComponentsManager { get; private set; }

    internal async Task HandleConnectionAsync()
    {
        try
        {
            NetworkStream connectionStream = _tcpClient.GetStream();

            _rpc = new JsonRpc(connectionStream, connectionStream);

            _rpc.AddLocalRpcTarget<IPreviewAppControllerService>(this, null);
            AppService = _rpc.Attach<IPreviewAppService>();

            try
            {
                _rpc.StartListening();
            }
            catch
            {
                _rpc.Dispose();
                throw;
            }

            // Handle JSON-RPC method calls and notifications on this connection
            await _rpc.Completion;
        }
        catch (IOException)
        {
            // The client disconnected abruptly
        }
        catch (RemoteInvocationException)
        {
            // There was an error in the JSON-RPC protocol
        }
        finally
        {
            _appManager?.RemoveAppConnection(this);
            _tcpClient.Dispose();
        }
    }

    public async Task RegisterAppAsync(string projectPath, string platformName)
    {
        if (_appManager is not null)
        {
            throw new InvalidOperationException($"App was already registered for this connection");
        }

        PlatformName = platformName;

        _appManager = _appsManager.GetOrCreateApp(projectPath);
        _appManager.AddAppConnection(this);

        UIComponentInfo[] uiComponentInfos = await AppService!.GetUIComponentsAsync();
        UIComponentsManager = new GetUIComponentsFromProtocol(uiComponentInfos).ToImmutable();

        _appManager.UpdateUIComponents();
    }

    public async Task NotifyUIComponentsChangedAsync()
    {
        UIComponentInfo[] uiComponentInfos = await AppService!.GetUIComponentsAsync();
        UIComponentsManager = new GetUIComponentsFromProtocol(uiComponentInfos).ToImmutable();

        _appManager?.UpdateUIComponents();
    }
}
