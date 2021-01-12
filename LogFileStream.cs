using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

namespace ConsoleFile
{
    public enum SensitiveType
    {
        Ip = 0,
        UserName = 1,
        Port = 2,
        UncPath = 3,
        Url = 4,
        Host = 5,
        IpOrHost = 6, 
        HostHeader = 7,
        DataBaseName = 8,
        DomainName = 9,
    }
    public class LogRetrieveDto
    {
        public SensitiveType Type { get; set; }
        
        public string OldString { get; set; }
        public string NewString { get; set; }
        public bool IsWildCard { get; set; }
    }
    class LogFileStream: FileStream
    {
        #region Members
        private StreamReader _reader;
        private byte[] _buffer;
        private int _bufferlength;
        private int _readIndex;
        private readonly int[] _offsets;
        private bool first;
        private long _block;
        private long _allBlock;
        private long _lave;
        private List<LogRetrieveDto> _regexListDto;
        private List<LogRetrieveDto> _exactListDto;
        private int _count = 1024;
        private long length = 0;
        private int _size = 5;
        private long _start;
        #endregion

        public LogFileStream(string path, FileMode mode, FileAccess access, FileShare share, List<LogRetrieveDto> listDto)
           : base(path, mode, access, share)
        {
            Console.WriteLine("path:{0}", path);
            var _listDto = listDto;
            _regexListDto = new List<LogRetrieveDto>();
            _exactListDto = new List<LogRetrieveDto>();
            foreach (var item in _listDto)
            {
                if (item.IsWildCard)
                {
                    _regexListDto.Add(item);
                }
                else
                {
                    _exactListDto.Add(item);
                }
            }
            _reader = new StreamReader(new FileStream(path, mode, access, share), Encoding.UTF8);
            _offsets = new int[_exactListDto.Count];
            first = true;
        }

        public override int Read(byte[] array, int offset, int count)
        {
            if (count > array.Length)
            {
                count = array.Length;
            }
            for (int i = 0; i < count && offset < count; i++)
            {
                if (_readIndex == _bufferlength)
                {
                    if (!FillBuffer())
                    {
                        return i;
                    }
                    _readIndex = 0;
                }
                array[offset] = _buffer[_readIndex];
                offset++;
                _readIndex++;
            }
            return count;
        }
        private bool FillBuffer()
        {
            try
            {
                if (dd == 100)
                {
                    var ff = 0;
                }
                length = _reader.BaseStream.Length;
                string line = string.Empty;
                //int length = 0;
                int bufferSize = 1024 * 1024 * _size;
                // 是否读取下一行
                if (length < _size * 1024 * 1024)
                {
                    line = _reader.ReadToEnd();
                    //line = _reader.ReadLine();
                    string _lineString = line;
                    //if (line == null) return false;
                    if (string.IsNullOrEmpty(line))
                    {
                        return false;
                    }
                }
                else
                {
                    if (first)
                    {
                        // 需要读几次10M块
                        _block = length / bufferSize;
                        _allBlock = length / bufferSize;
                        // 剩余不足10M的数据长度
                        _lave = length % bufferSize;
                        first = false;
                        _reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    }
                    if (_block < 0)
                    {
                        //_start = 0;
                        _block = 0;
                        _lave = 0;
                        _allBlock = 0;
                        return false;
                    }
                    if (_block == 0)
                    {
                        bufferSize = (int)_lave;
                        if (bufferSize == 0) return false;
                    }
                    // 读取块
                    int read = 0;
                    if (_allBlock == _block)
                    {
                        _reader.DiscardBufferedData();
                    }
                    _start = _reader.BaseStream.Position;

                    if (_block == 1)
                    {
                        _lave = _allBlock * 1024 * 1024 * _size + _lave - _start;
                        if (_lave < 0)
                        {
                            _start = 0;
                            _block = 0;
                            _lave = 0;
                            _allBlock = 0;
                            return false;
                        }
                    }
                    char[] buffer = new Char[bufferSize];
                    var dufferLength = buffer.Length;
                    read = _reader.Read(buffer, 0, dufferLength);
                    if (read <= 0)
                    {
                        _start = 0;
                        _block = 0;
                        _lave = 0;
                        _allBlock = 0;
                        return false;
                    }
                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (char c in buffer)
                    {
                        if (read == 20)
                        {
                            var ff = c;
                        }
                        stringBuilder.Append(c);
                        read--;
                        if (read == 0)
                        {
                            break;
                        }
                    }
                    line = stringBuilder.ToString();
                    //line = new string(buffer);
                    // 前后取固定长度信息 校验是否有敏感信息
                    string checkString = string.Empty;
                    char[] chars = new char[2 * 1024];
                    if (_allBlock == _block)
                    {
                        if ((bufferSize - _count + 2 * _count) > length)
                        {
                            _reader.BaseStream.Seek(bufferSize - _count, SeekOrigin.Begin);
                            _reader.Read(chars, 0, (int)length - bufferSize);
                            checkString = new string(chars);
                        }
                        else
                        {
                            _reader.BaseStream.Seek(bufferSize - _count, SeekOrigin.Begin);
                            _reader.Read(chars, 0, 2 * _count);
                            checkString = new string(chars);
                        }
                    }
                    else
                    {

                        if ((_start - _count + 2 * _count) > length)
                        {
                            _reader.BaseStream.Seek(_start, SeekOrigin.Begin);
                            _reader.Read(chars, 0, (int)(length - _start));
                            checkString = new string(chars);
                        }
                        else
                        {
                            _reader.BaseStream.Seek(_start - _count, SeekOrigin.Begin);
                            _reader.Read(chars, 0, 2 * _count);
                            checkString = new string(chars);
                        }
                    }
                    int _match = CheckString(checkString);
                    if (_match > -1)
                    {
                        bufferSize = bufferSize - _count + _match;
                    }
                    _start += bufferSize;
                    _reader.BaseStream.Seek(_start, SeekOrigin.Begin);
                    _lave = length - bufferSize;
                }
                Thread.Sleep(500);
                StringBuilder result = ReplaceString(line);

                int maxByteCount = result.Length * 2;
                if (_buffer == null || _buffer.Length < maxByteCount)
                {
                    _buffer = new byte[maxByteCount];
                }
                _bufferlength = Encoding.UTF8.GetBytes(result.ToString(), 0, result.Length, _buffer, 0);
                _block--;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return true;
        }
        //private bool FillBuffer()
        //{
        //    string line = _reader.ReadLine();
        //    if (line == null) return false;
        //    StringBuilder result = ReplaceString(line);
        //    result.Append(Environment.NewLine);
        //    int maxByteCount = result.Length * 4;
        //    if (_buffer == null || _buffer.Length < maxByteCount)
        //    {
        //        _buffer = new byte[maxByteCount];
        //    }
        //    _bufferlength = Encoding.UTF8.GetBytes(result.ToString(), 0, result.Length, _buffer, 0);
        //    return true;
        //}

        private StringBuilder sBuilder = new StringBuilder(1024 * 256);
        int dd = 0;
        private StringBuilder ReplaceString(string line)
        {
            try
            {
                if (string.IsNullOrEmpty(line))
                {
                    return sBuilder;
                }
                if (sBuilder.Length != 0)
                {
                    sBuilder.Remove(0, sBuilder.Length);
                }
                Array.Clear(_offsets, 0, _offsets.Length);
                string lowerLine = line.ToLower(CultureInfo.CurrentCulture);
                int mi = -1;
                int wi = -1;
                int ti = 0;
                dd++;
                if (_regexListDto.Count > 0)
                {
                    foreach (var item in _regexListDto)
                    {
                        Stopwatch watch = new Stopwatch();
                        watch.Start();
                        line = Regex.Replace(line, @item.OldString, item.NewString, RegexOptions.IgnoreCase);

                        watch.Stop();
                        Console.WriteLine("Regex.Replace used: {0} Regular expression: {1} === {2}   ----  {3}", watch.Elapsed.Seconds, @item.OldString, watch.Elapsed.TotalSeconds, dd);
                    }
                }

                while (true)
                {
                    mi = -1;
                    for (int i = 0; i < _exactListDto.Count; i++)
                    {
                        if (_offsets[i] != -1 && _offsets[i] <= ti)
                        {
                            _offsets[i] = lowerLine.IndexOf(_exactListDto[i].OldString, ti, StringComparison.Ordinal);
                        }
                        if (_offsets[i] >= 0 && (_offsets[i] < mi || mi == -1))
                        {
                            mi = _offsets[i];
                            wi = i;
                        }
                    }
                    if (mi >= 0)
                    {
                        if (mi > 0)
                        {
                            sBuilder.Append(line, ti, mi - ti);
                        }
                        sBuilder.Append(_exactListDto[wi].NewString);
                        ti = (mi + _exactListDto[wi].OldString.Length);
                    }
                    else
                    {
                        sBuilder.Append(line, ti, line.Length - ti);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return sBuilder;
        }

        private int CheckString(string line)
        {
            int checkIndex = -1;
            if (_regexListDto.Count > 0)
            {
                foreach (var item in _regexListDto)
                {
                    var index = Regex.Match(line, @item.OldString);
                    if (Regex.Match(line, @item.OldString).Success)
                    {
                        checkIndex = index.Index;
                        break;
                    }
                }

            }
            if (_exactListDto.Count > 0)
            {
                foreach (var item in _exactListDto)
                {
                    int index = line.IndexOf(@item.OldString);
                    if (index > -1)
                    {
                        checkIndex = index;
                        break;
                    }
                }
            }
            return checkIndex;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_reader != null)
            {
                _reader.Dispose();
            }
            _buffer = null;
            _reader = null;
            sBuilder = null;
            _regexListDto = null;
            _exactListDto = null;
        }
    }
}


