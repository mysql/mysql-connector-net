using System;
using System.Collections;
using System.Collections.Generic;
using MySql.Protocol;
using MySql.Properties;

namespace MySql.XDevAPI.Results
{
  public abstract class BufferingResult<T> : Result, IEnumerable<T>, IEnumerator<T>
  {
    int _position;
    List<T> _items = new List<T>();
    protected bool _isComplete;
    protected ProtocolBase _protocol;
    protected bool _autoClose;

    internal BufferingResult(ProtocolBase protocol, bool autoClose)
    {
      _protocol = protocol;
      _autoClose = autoClose;
      PageSize = 20;
      _position = -1;
    }

    protected int Position
    {
      get { return _position;  }
    }

    protected List<T> Items
    {
      get { return _items;  }
    }

    public int PageSize { get; private set; }

    public void Buffer()
    {
      while (!_isComplete)
        PageInItems() ;
    }

    internal void Dump()
    {
      if (_isComplete) return;
      while (true)
      {
        if (ReadItem(true) == null) break;
      }
      _isComplete = true;
    }

    public bool Next()
    {
      _position++;
      if (Position == _items.Count)
      {
        if (_isComplete) return false;
        if (!PageInItems())
        {
          _isComplete = true;
          return false;
        }
      }
      return true;
    }

    protected abstract T ReadItem(bool dumping);

    private bool PageInItems()
    {
      int count = 0;
      for (int i = 0; i < PageSize; i++)
      {
        T item = ReadItem(false);
        if (item == null)
        {
          _isComplete = true;
          break;
        }
        _items.Add(item);
        count++;
      }
      return count > 0;
    }

    public void Dispose()
    {
    }

    public T Current
    {
      get 
      {
        if (_position == _items.Count)
          throw new InvalidOperationException(String.Format(Resources.NoDataAtIndex, _position));
        return _items[_position];
      }
    }

    object IEnumerator.Current
    {
      get { return this.Current;  }
    }

    public bool MoveNext()
    {
      return Next();
    }

    public void Reset()
    {
      _position = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
      return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this;
    }
  }
}
