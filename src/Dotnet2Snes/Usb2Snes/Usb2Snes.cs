using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Dotnet2Snes.Usb2Snes
{
    public class Usb2Snes : IDisposable
    {
        public Usb2Snes(State state, bool isSd2Snes, int port)
        {
            this.State = state;
            this.IsSd2Snes = isSd2Snes;
            this.Port = port;

        }

        public State State { get; private set; }

        /// <summary>
        /// If this is an SD2SNES instance
        /// </summary>
        public bool IsSd2Snes { get; private set; }

        public int Port { get; set; }

        public const int ReceiveBufferSize = 8192;

        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private readonly BufferBlock<Response> _responses;
        private readonly BufferBlock<byte[]> _data;
        private readonly SemaphoreSlim _responses_lock;

        public Usb2Snes(int port = 8080)
        {
            State = State.SNES_DISCONNECTED;
            Port = port;
            IsSd2Snes = false;
            _cts = new CancellationTokenSource();
            _responses = new BufferBlock<Response>();
            _data = new BufferBlock<byte[]>();
            _responses_lock = new SemaphoreSlim(1, 1);
        }

        public void Dispose() => Disconnect().Wait();

        public async Task Connect()
        {
            if (_ws != null)
            {
                if (_ws.State == WebSocketState.Open) return;
                else _ws.Dispose();
            }

            _ws = new ClientWebSocket();
            Uri uri = new Uri("ws://localhost:" + Port.ToString());
            State = State.SNES_CONNECTING;
            try
            {
                await _ws.ConnectAsync(uri, _cts.Token);
                await Task.Factory.StartNew(ReceiveLoop, _cts.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default);
                State = State.SNES_CONNECTED;
            }
            catch
            {
                if (_ws != null)
                    await _ws.CloseAsync(
                        WebSocketCloseStatus.InternalServerError,
                        "Exception occurred",
                        _cts.Token
                    );
                _ws = null;
                State = State.SNES_DISCONNECTED;
            }
        }

        public async Task Disconnect()
        {
            if (_ws == null) return;

            if (_ws.State == WebSocketState.Open)
            {
                _cts.CancelAfter(TimeSpan.FromSeconds(2));
                await _ws.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "",
                    CancellationToken.None
                );
                await _ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "",
                    CancellationToken.None
                );
            }
            _ws.Dispose();
            _ws = null;
            _cts.Dispose();
            _cts = null;
        }

        public async Task<List<string>> GetDeviceList()
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                Command command = new Command(OpCodes.DeviceList, null, null);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );

                var r = await _responses.ReceiveAsync<Response>(_cts.Token);
                return r.Results;
            }
            catch (TaskCanceledException) { return new List<string>(); }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task Attach(string device)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string> { device };
                Command command = new Command(OpCodes.Attach, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );

                State = State.SNES_CONNECTED;
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task<string> AppVersion()
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                Command command = new Command(OpCodes.AppVersion, null, null);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );

                var r = await _responses.ReceiveAsync<Response>(_cts.Token);
                return r.Results.FirstOrDefault();
            }
            catch (TaskCanceledException) { return string.Empty; }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task Name()
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                Command command = new Command(OpCodes.Name, null,
                    new List<string>{"Dotnet2SnesClient"});
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task<List<string> > Info()
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                Command command = new Command(OpCodes.Info, null, null);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );

                var r = await _responses.ReceiveAsync(_cts.Token);
                return r.Results;
            }
            catch (TaskCanceledException) { return new List<string>(); }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task Boot(string romPath)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string>() { romPath };
                Command command = new Command(OpCodes.Boot, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task Menu()
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                Command command = new Command(OpCodes.Menu, null, null);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task Reset()
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                Command command = new Command(OpCodes.Reset, null, null);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

#pragma warning disable CS1998
        public async Task Binary()
        {
            CheckConnection();
            throw new NotImplementedException("Binary is not yet supported.");
        }

        public async Task Stream()
        {
            CheckConnection();
            throw new NotImplementedException("Stream is not yet supported.");
        }

        public async Task Fence()
        {
            CheckConnection();
            throw new NotImplementedException("Fence is not yet supported.");
        }
#pragma warning restore CS1998

        public async Task<byte[]> GetAddress(int offset, int size)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string>() 
                    { offset.ToString(), size.ToString()};
                var command = new Command(OpCodes.GetAddress, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );

                var r = await _responses.ReceiveAsync(_cts.Token);
                var data = await _data.ReceiveAsync(_cts.Token);
                // TODO: Check the data length matches the size specified
                // in the response
                return data;
            }
            catch (TaskCanceledException) { return null; }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task PutAddress(int offset, byte[] data)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string>() 
                    { offset.ToString(), data.Length.ToString() };
                var command = new Command(OpCodes.PutAddress, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );

                // TODO: Support sending multiple putaddress in one call
                await _ws.SendAsync(
                    data,
                    WebSocketMessageType.Binary,
                    true,
                    _cts.Token);
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task PutIps(string name, byte[] data)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string>() 
                    { name, data.Length.ToString() };
                var command = new Command(OpCodes.PutIPS, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );

                await _ws.SendAsync(
                    data,
                    WebSocketMessageType.Binary,
                    true,
                    _cts.Token);
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task<byte[]>GetFile(string filepath)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string>() { filepath };
                var command = new Command(OpCodes.GetFile, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );

                var r = await _responses.ReceiveAsync(_cts.Token);
                var data = await _data.ReceiveAsync(_cts.Token);
                return data;
            }
            catch (TaskCanceledException) { return null; }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task PutFile(string filepath, byte[] file)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string>()
                    { filepath, file.Length.ToString() };
                var command = new Command(OpCodes.PutFile, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );

                await _ws.SendAsync(
                    file,
                    WebSocketMessageType.Binary,
                    true,
                    _cts.Token);
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task<(List<string>, List<string>)> List(string dirpath)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string>() { dirpath };
                var command = new Command(OpCodes.List, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );

                var r = await _responses.ReceiveAsync(_cts.Token);
                var filetypes = new List<string>();
                var filenames = new List<string>();
                for (int i = 0; i < r.Results.Count; i+= 2)
                {
                    filetypes.Add(r.Results[i]);
                    filenames.Add(r.Results[i+1]);
                }
                return (filetypes, filenames);
            }
            catch (TaskCanceledException) { return (null, null); }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task Remove(string filepath)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string>() { filepath };
                var command = new Command(OpCodes.Remove, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task Rename(string filepath, string newfilename)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string>() { filepath, newfilename };
                var command = new Command(OpCodes.Rename, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

        public async Task MakeDir(string filepath)
        {
            CheckConnection();
            await _responses_lock.WaitAsync();
            try
            {
                var operands = new List<string>() { filepath };
                var command = new Command(OpCodes.MakeDir, null, operands);
                await _ws.SendAsync(
                    command.ToBytes(),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );
            }
            catch (TaskCanceledException) { }
            finally
            {
                _responses_lock.Release();
            }
        }

        private async Task ReceiveLoop()
        {
            var loopToken = _cts.Token;
            MemoryStream outputStream = null;
            WebSocketReceiveResult rcvResult;
            var buffer = new byte[ReceiveBufferSize];
            try
            {
                while (!loopToken.IsCancellationRequested)
                {
                    outputStream = new MemoryStream(ReceiveBufferSize);
                    rcvResult = await _ws.ReceiveAsync(buffer, _cts.Token);
                    outputStream.Write(buffer, 0, rcvResult.Count);
                    bool isBinary = 
                        rcvResult.MessageType == WebSocketMessageType.Binary;
                    while (!rcvResult.EndOfMessage)
                    {
                        if (rcvResult.MessageType !=
                                WebSocketMessageType.Close)
                            outputStream.Write(buffer, 0, rcvResult.Count);
                    }
                    if (rcvResult.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    outputStream.Position = 0;
                    ResponseReceived(outputStream, isBinary);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                outputStream?.Dispose();
            }
        }

        private void ResponseReceived(Stream inputStream, bool isBinary)
        {
            try
            {
                if (isBinary)
                {
                    byte[] data = new byte[inputStream.Length];

                    // if you're reading more than 2 GB from an SNES,
                    // you're using the wrong tool
                    inputStream.Read(data, 0, (int)inputStream.Length);
                    _data.SendAsync(data);
                }
                else
                {
                    JsonSerializer j = new JsonSerializer();
                    using StreamReader sr = new StreamReader(inputStream);
                    using JsonTextReader jr = new JsonTextReader(sr);
                    Response r = j.Deserialize<Response>(jr);
                    _responses.SendAsync(r).Wait();
                }
            }
            catch
            {

            }
        }

        private void CheckConnection()
        {
            if ((State != State.SNES_CONNECTED) ||
                (_ws is null) || (_ws.State != WebSocketState.Open))
            {
                throw new WebSocketException("Not currently connected.");
            }
        }
    }

    public enum State
    {
        SNES_DISCONNECTED,
        SNES_CONNECTING,
        SNES_CONNECTED,
        SNES_ATTACHED
    }
}