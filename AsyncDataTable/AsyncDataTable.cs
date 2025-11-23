namespace AsyncDataTable;

using System;
using System.Threading.Channels;

public class AsyncDataTable<T>
{
    public enum Command
    {
        Insert,
        Update,
        Delete,
        Upsert,
        Clear,
    }

    public record struct IndexedData(int Index, T Data);
    public record struct CommandedData(Command Command, T Item, Predicate<T>? predicate, TaskCompletionSource<int>? tcs);

    public AsyncDataTable()
    {
        _commandChannel = Channel.CreateUnbounded<CommandedData>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    }

    private readonly List<IndexedData> _datas = new();
    private int _dataLastIndex = -1;

    private readonly Channel<CommandedData> _commandChannel;

    public ValueTask InsertCommand(Command cmd, T data, Predicate<T>? predicate = null)
    {
        var cmdData = new CommandedData(cmd, data, predicate, null);
        return _commandChannel.Writer.WriteAsync(cmdData);
    }

    public Task<int> InsertCommandAsync(Command cmd, T data, Predicate<T>? predicate = null)
    {
        TaskCompletionSource<int> tcs = new();
        var cmdData = new CommandedData(cmd, data, predicate, tcs);
        _commandChannel.Writer.WriteAsync(cmdData);
        return tcs.Task;
    }

    public void ProcessCommand()
    {
        while (_commandChannel.Reader.TryRead(out var cmdData))
        {
            int foundIndex = -1;

            try
            {
                switch (cmdData.Command)
                {
                    case Command.Insert:
                        _datas.Add(new IndexedData(++_dataLastIndex, cmdData.Item));
                        cmdData.tcs?.SetResult(_dataLastIndex);
                        break;
                    case Command.Update:
                        if (cmdData.predicate != null)
                        {
                            for (int i = 0; i < _datas.Count; i++)
                            {
                                if (cmdData.predicate(_datas[i].Data))
                                {
                                    foundIndex = _datas[i].Index;
                                    _datas[i] = new IndexedData(_datas[i].Index, cmdData.Item);
                                }
                            }
                        }

                        cmdData.tcs?.SetResult(foundIndex);
                        break;
                    case Command.Delete:
                        if (cmdData.predicate != null)
                        {
                            for (int i = 0; i < _datas.Count; i++)
                            {
                                if (cmdData.predicate(_datas[i].Data))
                                {
                                    foundIndex = _datas[i].Index;
                                    _datas.RemoveAt(i);
                                    break;
                                }
                            }
                        }

                        cmdData.tcs?.SetResult(foundIndex);
                        break;
                    case Command.Upsert:
                        if (cmdData.predicate != null)
                        {
                            bool found = false;
                            for (int i = 0; i < _datas.Count; i++)
                            {
                                if (cmdData.predicate(_datas[i].Data))
                                {
                                    foundIndex = _datas[i].Index;
                                    _datas[i] = new IndexedData(_datas[i].Index, cmdData.Item);
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                _datas.Add(new IndexedData(++_dataLastIndex, cmdData.Item));
                                foundIndex = _dataLastIndex;
                            }
                        }
                        else
                        {
                            _datas.Add(new IndexedData(++_dataLastIndex, cmdData.Item));
                            foundIndex = _dataLastIndex;
                        }

                        cmdData.tcs?.SetResult(foundIndex);
                        break;
                    case Command.Clear:
                        _datas.Clear();
                        cmdData.tcs?.SetResult(foundIndex);
                        break;
                }
            }
            catch (Exception ex)
            {
                cmdData.tcs?.SetException(ex);
            }
        }
    }
}