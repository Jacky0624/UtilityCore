using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCore.Communication.Alarm;

namespace UtilityCore.Communication.Serial
{
    public struct IsFullPacketResult
    {
        internal readonly bool IsFullPacket;
        internal readonly int CurrentIndex;

        public IsFullPacketResult(bool isFullPacket, int currentIndex)
        {
            IsFullPacket = isFullPacket;
            CurrentIndex = currentIndex;
        }
    }

    public class SerialHelper
    {
        internal event Action<byte[]> OnSendBytes;
        internal event Action<string> OnReceiveString;
        internal event Action<byte[]> OnReceiveBytes;
        internal event Action<AlarmBase> OnAlarm;

        internal Func<byte[], IsFullPacketResult> CheckIsFullPacket = null; //傳回完整封包長度，若不完整，傳回0

        private readonly object _accessLock = new object();

        private byte[] _buffer = new byte[10000];
        private int _bufferBytesNow = 0;

        private readonly System.Timers.Timer _receiveTimeoutTimer = new System.Timers.Timer();

        private bool _busy = false;

        private Action<byte[]> _sendMethod = null;

        private TaskCompletionSource<bool> _sendBytesWithoutResponseComplete = new TaskCompletionSource<bool>();
        private TaskCompletionSource<byte[]> _sendBytesWithResponseComplete = new TaskCompletionSource<byte[]>();

        private SerialSetting _setting = null;
        public SerialSetting Setting
        {
            get
            {
                return _setting;
            }
            set
            {
                _setting = value;
                _setting.PropertyChanged += _setting_PropertyChanged;

                lock (_accessLock)
                {
                    if (_receiveTimeoutTimer.Interval != _setting.TimeoutMs)
                    {
                        _receiveTimeoutTimer.Interval = _setting.TimeoutMs;
                    }
                }
            }
        }

        private void _setting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TimeoutMs")
            {
                int newTimeoutMs = _setting.TimeoutMs;
                lock (_accessLock)
                {
                    if (_receiveTimeoutTimer.Interval != newTimeoutMs)
                    {
                        _receiveTimeoutTimer.Interval = newTimeoutMs;
                    }
                }
            }
        }

        internal SerialHelper(Action<byte[]> sendMethod)
        {
            _sendMethod = sendMethod;

            lock (_accessLock)
            {
                _receiveTimeoutTimer.Interval = 250;
                _receiveTimeoutTimer.AutoReset = false;
                _receiveTimeoutTimer.Elapsed += _receiveTimeoutTimer_Elapsed;
            }

            _sendBytesWithoutResponseComplete.TrySetResult(true);
            _sendBytesWithResponseComplete.TrySetResult(null);
        }


        internal void SendString(string message)
        {
            byte[] bytes = GetBytesWithEof(message);

            if (OnSendBytes != null)
            {
                OnSendBytes(bytes);
            }

            _sendMethod(bytes);
        }

        internal async Task<string> SendStringAsync(string message, int timeoutMs = -1)
        {
            byte[] bytes = Setting.Encoding.GetBytes(message);
            byte[] responseBytes = await SendBytesAsync(bytes, timeoutMs);
            if (responseBytes != null)
            {
                string responseString = Setting.Encoding.GetString(responseBytes, 0, responseBytes.Length - Setting.ReceiveStringEndOfLine.Length);
                return responseString;
            }
            else
            {
                return null;
            }
        }

        internal async Task SendStringAsyncNoResponse(string message, int completeTimeMs = 500)
        {
            byte[] bytes = Setting.Encoding.GetBytes(message);
            await SendBytesAsyncNoResponse(bytes, completeTimeMs);
        }


        internal void SendBytes(byte[] bytes)
        {
            byte[] bytesWithEof = GetBytesWithEof(bytes);

            _sendMethod(bytesWithEof);

            if (OnSendBytes != null)
            {
                OnSendBytes(bytesWithEof);
            }
        }

        internal async Task<byte[]> SendBytesAsync(byte[] bytes, int timeoutMs = -1)
        {
            bool hasAnotherThread = false;
            if (timeoutMs < 0)
            {
                timeoutMs = Setting.TimeoutMs;
            }
            S:
            await _sendBytesWithResponseComplete.Task;

            bool busyCopy = false;
            lock (_accessLock)
            {
                if (!hasAnotherThread)
                {
                    busyCopy = (_busy);
                }
                if (!_busy)
                {
                    _busy = true;
                    _sendBytesWithResponseComplete = new TaskCompletionSource<byte[]>();
                }
            }

            if (busyCopy)
            {
                hasAnotherThread = true;
                goto S;
            }

            byte[] response = null;

            byte[] bytesWithEof = GetBytesWithEof(bytes);

            if (OnSendBytes != null)
            {
                OnSendBytes(bytesWithEof);
            }

            _receiveTimeoutTimer.Start();

            try
            {
                _sendMethod(bytesWithEof);
            }
            catch (Exception ex)
            {
                lock (_accessLock)
                {
                    _busy = false;
                    Task.Factory.StartNew(() =>
                    {
                        _sendBytesWithResponseComplete.TrySetResult(null);
                    }, TaskCreationOptions.LongRunning);
                }
                throw ex;
            }

            await _sendBytesWithResponseComplete.Task;
            response = _sendBytesWithResponseComplete.Task.Result;

            lock (_accessLock)
            {
                _busy = false;

                //if (response == null)
                //{
                //	throw new TimeoutException(string.Format("超過 {0} ms沒有回應", timeoutMs));
                //}
            }

            return response;
        }

        internal async Task SendBytesAsyncNoResponse(byte[] bytes, int completeTimeMs = 500)
        {
            bool hasAnotherThread = false;
            S:
            await _sendBytesWithoutResponseComplete.Task;

            bool busyCopy = false;
            lock (_accessLock)
            {
                if (!hasAnotherThread)
                {
                    busyCopy = _busy;
                }
                if (!_busy)
                {
                    _busy = true;
                    _sendBytesWithoutResponseComplete = new TaskCompletionSource<bool>();
                }
            }

            if (busyCopy)
            {
                hasAnotherThread = true;
                goto S;
            }

            byte[] bytesWithEof = GetBytesWithEof(bytes);

            if (OnSendBytes != null)
            {
                OnSendBytes(bytesWithEof);
            }

            try
            {
                _sendMethod(bytesWithEof);
            }
            catch (Exception ex)
            {
                lock (_accessLock)
                {
                    _busy = false;
                    _sendBytesWithoutResponseComplete.TrySetResult(false);
                }
                //throw ex;
            }

            await Task.Delay(completeTimeMs);

            lock (_accessLock)
            {
                _busy = false;
                _sendBytesWithoutResponseComplete.TrySetResult(true);
            }
        }
        internal void OnConnectionStateChangedInternal(bool connected) //TODO
        {

        }
        internal void OnReceiveBytesInternal(byte[] bytes, int lengthThisTime)
        {
            try
            {
                if (lengthThisTime > 0)
                {
                    lock (_accessLock)
                    {
                        _receiveTimeoutTimer.Start();

                        Array.Copy(bytes, 0, _buffer, _bufferBytesNow, lengthThisTime);

                        _bufferBytesNow += lengthThisTime;

                        byte[] answerCopy = null;
                        int indexNow = 0;
                        int bytesRemainNow = _bufferBytesNow;
                        bool didGetFullPacket = false;

                        while (indexNow < _buffer.Length && indexNow < _bufferBytesNow)
                        {
                            didGetFullPacket = false;

                            int eofIndex = -1;

                            if (Setting.CheckFullPacketMode == CheckFullPacketModes.Custom)
                            {
                                int answerLength = 0;
                                IsFullPacketResult result = new IsFullPacketResult(true, bytesRemainNow);
                                try
                                {
                                    if (CheckIsFullPacket == null)
                                    {
                                        answerLength = bytesRemainNow;
                                        OnAlarm(new InvalidOperation("自訂檢查完整封包模式下，CheckIsFullPacket不可以是null"));
                                    }
                                    else
                                    {
                                        byte[] bytesCopy = new byte[bytesRemainNow];
                                        Array.Copy(_buffer, indexNow, bytesCopy, 0, bytesRemainNow);
                                        result = CheckIsFullPacket(bytesCopy);
                                        answerLength = result.CurrentIndex;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    answerLength = bytesRemainNow;
                                    OnAlarm(new CheckFullPacketImplementationException("CheckIsFullPacket Exception, {0}", ex));
                                }

                                if (result.IsFullPacket)
                                {
                                    if (answerLength <= 0 || answerLength > bytesRemainNow)
                                    {
                                        answerLength = bytesRemainNow;
                                        OnAlarm(new CheckFullPacketImplementationException("CheckIsFullPacket Exception: 傳回的CurrentIndex不合法"));
                                    }
                                    else
                                    {
                                        answerCopy = new byte[answerLength];
                                        Array.Copy(_buffer, indexNow, answerCopy, 0, answerLength);
                                    }
                                    indexNow = answerLength;
                                    bytesRemainNow = _bufferBytesNow - indexNow;

                                    didGetFullPacket = true;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (Setting.CheckFullPacketMode == CheckFullPacketModes.Timeout)
                            {
                                //由timeout處理
                                break;
                            }
                            else if (Setting.CheckFullPacketMode == CheckFullPacketModes.EOF)
                            {
                                int answerLength = 0;
                                eofIndex = FindFullPacketCustomEof(_buffer, indexNow, bytesRemainNow, ref answerLength, Setting.ReceiveStringEndOfLine);
                                if (eofIndex >= 0)
                                {
                                    didGetFullPacket = true;

                                    answerCopy = new byte[answerLength];
                                    Array.Copy(_buffer, indexNow, answerCopy, 0, answerLength);

                                    indexNow = eofIndex + Setting.ReceiveStringEndOfLine.Length;
                                    bytesRemainNow = _bufferBytesNow - indexNow;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (Setting.CheckFullPacketMode == CheckFullPacketModes.URC)
                            {
                                int answerLength = 0;
                                eofIndex = FindFullPacketURCEof(_buffer, indexNow, bytesRemainNow, ref answerLength, Setting.ReceiveStringEndOfLine);
                                if (eofIndex >= 0)
                                {
                                    didGetFullPacket = true;
                                    answerCopy = new byte[answerLength];
                                    Array.Copy(_buffer, indexNow, answerCopy, 0, answerLength);
                                    indexNow = eofIndex + Setting.ReceiveStringEndOfLine.Length;
                                    bytesRemainNow = _bufferBytesNow - indexNow;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (didGetFullPacket)
                            {
                                _receiveTimeoutTimer.Stop();
                                CallOnReceiveBytesAndString(answerCopy);
                            }
                        }
                        //將剩餘不完整封包的資料移到最前面
                        for (int whichIndex = indexNow; whichIndex < _bufferBytesNow; ++whichIndex)
                        {
                            _buffer[whichIndex - indexNow] = _buffer[whichIndex];
                        }
                        _bufferBytesNow = bytesRemainNow;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Log.WriteLine(ex.ToString());
            }
        }

        private void _receiveTimeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_accessLock)
            {
                _receiveTimeoutTimer.Stop();
            }

            if (Setting.CheckFullPacketMode == CheckFullPacketModes.Custom)
            {
                //CallOnAlarm(new ReceiveTimeout("Receive Timeout (Custom Mode)"));
            }
            else if (Setting.CheckFullPacketMode == CheckFullPacketModes.EOF)
            {
                //CallOnAlarm(new ReceiveTimeout("Receive Timeout (EOF Mode)"));
            }
            else if (Setting.CheckFullPacketMode == CheckFullPacketModes.Timeout)
            {

            }

            byte[] bytesCopy = null;

            lock (_accessLock)
            {
                if (_bufferBytesNow > 0)
                {
                    bytesCopy = new byte[_bufferBytesNow];
                    Array.Copy(_buffer, 0, bytesCopy, 0, _bufferBytesNow);
                }
                else
                {
                    bytesCopy = null;
                }

                _bufferBytesNow = 0;
            }

            CallOnReceiveBytesAndString(bytesCopy);
        }

        private void CallOnReceiveBytesAndString(byte[] bytes)
        {
            Task.Factory.StartNew(() =>
            {

                try
                {
                    if (OnReceiveBytes != null)
                    {
                        OnReceiveBytes(bytes);
                    }
                }
                catch (Exception ex)
                {
                    CallOnAlarm(new CallBackException("Call OnReceiveBytes Exception, {0}", ex));
                }

                string message = null;
                if (bytes != null)
                {
                    if (Setting.ReceiveStringEndOfLine == null
                        || Setting.ReceiveStringEndOfLine.Length <= 0
                        || Setting.ReceiveStringEndOfLine.Length > bytes.Length)
                    {
                        message = Setting.Encoding.GetString(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        message = Setting.Encoding.GetString(bytes, 0, bytes.Length - Setting.ReceiveStringEndOfLine.Length);
                    }
                }

                try
                {
                    if (OnReceiveString != null)
                    {
                        OnReceiveString(message);
                    }
                }
                catch (Exception ex)
                {
                    CallOnAlarm(new CallBackException("Call OnReceiveString Exception", ex));
                }

                try
                {
                    _sendBytesWithResponseComplete.TrySetResult(bytes);
                }
                catch (Exception ex)
                {
                    CallOnAlarm(new CallBackException("Call _sendBytesComplete.TrySetResult(bytes) Exception", ex));
                }


            });
            //}, TaskCreationOptions.LongRunning);
        }

        private void CallOnAlarm(AlarmBase alarm)
        {
            try
            {
                if (OnAlarm != null)
                {
                    OnAlarm(alarm);
                }
            }
            catch (Exception ex)
            {
                //CallOnAlarm(new CallBackException("Call OnAlarm Exception", ex));
            }
        }
        private static int FindFullPacketURCEof(byte[] data, int startIndex, int length, ref int answerLength, byte[] eof)
        {
            answerLength = 0;
            int eofEndIndex = -1;
            for (int i = 0; i < length; ++i)
            {
                int currentIndex = startIndex + i;

                for (int whichEof = 0; whichEof < eof.Length; ++whichEof)
                {
                    int currentEofIndex = currentIndex + whichEof;
                    if (currentEofIndex >= startIndex + length)
                    {
                        goto END;
                    }
                    else if (data[currentIndex + whichEof] == eof[whichEof])
                    {
                        if (whichEof >= eof.Length - 1)
                        {

                            var correctLength = (data[4 + startIndex] | data[5 + startIndex] << 8) + 8 + startIndex;
                            if (correctLength == currentIndex)
                            {
                                eofEndIndex = currentIndex;
                                answerLength = currentIndex - startIndex + eof.Length;
                                goto END;
                            }
                            else if (correctLength < currentIndex)
                            {

                                //eofEndIndex = currentIndex;
                                //answerLength = currentIndex - startIndex + eof.Length;
                                //goto END;
                                //break;
                            }
                            else
                            {

                                whichEof = 0;
                                break;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            END:
            return eofEndIndex;
        }
        private static int FindFullPacketCustomEof(byte[] data, int startIndex, int length, ref int answerLength, byte[] eof)
        {
            answerLength = 0;
            int eofEndIndex = -1;
            for (int i = 0; i < length; ++i)
            {
                int currentIndex = startIndex + i;

                for (int whichEof = 0; whichEof < eof.Length; ++whichEof)
                {
                    int currentEofIndex = currentIndex + whichEof;
                    if (currentEofIndex >= startIndex + length)
                    {
                        goto END;
                    }
                    else if (data[currentIndex + whichEof] == eof[whichEof])
                    {
                        if (whichEof >= eof.Length - 1)
                        {
                            eofEndIndex = currentIndex;
                            answerLength = currentIndex - startIndex + eof.Length;
                            goto END;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            END:
            return eofEndIndex;
        }

        private byte[] GetBytesWithEof(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("不可傳送null");
            }
            else
            {
                byte[] bytes = Setting.Encoding.GetBytes(message);
                byte[] bytesWithEof = GetBytesWithEof(bytes);

                return bytesWithEof;
            }
        }

        private byte[] GetBytesWithEof(byte[] bytes)
        {
            byte[] bytesWithEof = null;
            if (Setting.SendStringEndOfLine == null || Setting.SendStringEndOfLine.Length <= 0 || Setting.CheckFullPacketMode == CheckFullPacketModes.Custom)
            {
                bytesWithEof = new byte[bytes.Length];
                Array.Copy(bytes, bytesWithEof, bytes.Length);
            }
            else
            {
                bytesWithEof = new byte[bytes.Length + Setting.SendStringEndOfLine.Length];
                Array.Copy(bytes, bytesWithEof, bytes.Length);
                Array.Copy(Setting.SendStringEndOfLine, 0, bytesWithEof, bytes.Length, Setting.SendStringEndOfLine.Length);
            }

            return bytesWithEof;
        }
    }
}
