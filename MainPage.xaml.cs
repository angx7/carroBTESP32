using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;

namespace ControlRemotoBT;

public partial class MainPage : ContentPage
{
    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;
    private IDevice _connectedDevice;
    private IService _bluetoothService;
    private ICharacteristic _carCharacteristic;

    private readonly Guid _serviceUuid = Guid.Parse("0000ffe0-0000-1000-8000-00805f9b34fb");
    private readonly Guid _characteristicUuid = Guid.Parse("0000ffe1-0000-1000-8000-00805f9b34fb");

    private const string TargetDeviceName = "ESP32_FullStackForce";

    public MainPage()
    {
        InitializeComponent();

        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;

        _adapter.DeviceDiscovered += OnDeviceDiscovered;
        _adapter.ScanTimeoutElapsed += OnScanTimeout;

        gridControles.IsEnabled = false;
    }

    private async Task SolicitarPermisosAsync()
    {
#if ANDROID
        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        await Permissions.RequestAsync<Permissions.Bluetooth>();
#endif
    }

    private void OnScanTimeout(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            lblEstatus.Text = "Dispositivo no encontrado";
            btnConectar.Text = "Conectar";
            btnConectar.IsEnabled = true;
        });
    }

    private async void OnDeviceDiscovered(object? sender, DeviceEventArgs e)
    {
        Console.WriteLine($"Dispositivo encontrado: {e.Device.Name}");

        if (e.Device.Name == TargetDeviceName)
        {
            await _adapter.StopScanningForDevicesAsync();

            try
            {
                if (_connectedDevice != null)
                {
                    await _adapter.DisconnectDeviceAsync(_connectedDevice);
                    _connectedDevice = null;
                }

                await Task.Delay(500);

                await _adapter.ConnectToDeviceAsync(
                    e.Device,
                    new ConnectParameters(autoConnect: false, forceBleTransport: true)
                );

                _connectedDevice = e.Device;
                lblEstatus.Text = "Conectado";

                var services = await _connectedDevice.GetServicesAsync();
                _bluetoothService = services.FirstOrDefault(s => s.Id == _serviceUuid);

                if (_bluetoothService != null)
                {
                    _carCharacteristic = await _bluetoothService.GetCharacteristicAsync(_characteristicUuid);
                    if (_carCharacteristic != null)
                    {
                        gridControles.IsEnabled = true;
                        btnConectar.Text = "Desconectar";
                    }
                }
            }
            catch (DeviceConnectionException ex)
            {
                await DisplayAlert("Error", $"No se pudo conectar: {ex.Message}", "OK");
                lblEstatus.Text = "Error de conexión";
                btnConectar.Text = "Conectar";
            }
            finally
            {
                btnConectar.IsEnabled = true;
            }
        }
    }

    private async void OnConnectivityClicked(object sender, EventArgs e)
    {
        if (_connectedDevice != null)
        {
            await DisconnectDevice();
            btnConectar.Text = "Conectar";
            gridControles.IsEnabled = false;
            return;
        }

        btnConectar.IsEnabled = false;
        btnConectar.Text = "Buscando...";
        lblEstatus.Text = "Escaneando dispositivos...";

        await SolicitarPermisosAsync();

        try
        {
            await _adapter.StartScanningForDevicesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al escanear: {ex.Message}", "OK");
            btnConectar.IsEnabled = true;
            btnConectar.Text = "Conectar";
        }
    }

    private async Task DisconnectDevice()
    {
        try
        {
            if (_connectedDevice != null)
            {
                await _adapter.DisconnectDeviceAsync(_connectedDevice);
                _connectedDevice = null;
                _bluetoothService = null;
                _carCharacteristic = null;
                lblEstatus.Text = "Desconectado";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al desconectar: {ex.Message}", "OK");
        }
    }

    private async Task EnviarComando(string comando)
    {
        if (_carCharacteristic == null || !_carCharacteristic.CanWrite)
        {
            await DisplayAlert("Error", "No se puede escribir en la característica", "OK");
            return;
        }

        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(comando);
            await _carCharacteristic.WriteAsync(bytes);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Fallo al enviar comando: {ex.Message}", "OK");
        }
    }

    // Eventos de control
    private async void OnMoveForwardClicked(object sender, EventArgs e) => await EnviarComando("F");
    private async void OnMoveBackwardClicked(object sender, EventArgs e) => await EnviarComando("B");
    private async void OnTurnLeftClicked(object sender, EventArgs e) => await EnviarComando("L");
    private async void OnTurnRightClicked(object sender, EventArgs e) => await EnviarComando("R");
    private async void OnStopClicked(object sender, EventArgs e) => await EnviarComando("S");
}

