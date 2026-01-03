namespace ELImGui.Base;

using System.Runtime.CompilerServices;

/// <summary>
/// 고정 크기 링 버퍼 - 메모리 할당 없이 고정된 크기로 순환하며 데이터 저장
/// </summary>
/// <typeparam name="T">저장할 데이터 타입 (struct 권장)</typeparam>
public class RingBuffer<T> where T : struct
{
    private readonly T[] _buffer;
    private readonly int _capacity;
    private int _head;      // 다음 쓰기 위치
    private int _count;     // 현재 저장된 데이터 개수

    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));
        }

        _capacity = capacity;
        _buffer = new T[capacity];
        _head = 0;
        _count = 0;
    }

    /// <summary>
    /// 버퍼의 최대 용량
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// 현재 저장된 데이터 개수
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// 버퍼가 가득 찼는지 여부
    /// </summary>
    public bool IsFull => _count == _capacity;

    /// <summary>
    /// 버퍼가 비어있는지 여부
    /// </summary>
    public bool IsEmpty => _count == 0;

    /// <summary>
    /// 데이터 추가 (가장 오래된 데이터를 덮어씀)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        _buffer[_head] = item;
        _head = (_head + 1) % _capacity;

        if (_count < _capacity)
        {
            _count++;
        }
    }

    /// <summary>
    /// 인덱스로 데이터 접근 (0 = 가장 오래된 데이터)
    /// </summary>
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int actualIndex = (_head - _count + index + _capacity) % _capacity;
            return _buffer[actualIndex];
        }
        set
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int actualIndex = (_head - _count + index + _capacity) % _capacity;
            _buffer[actualIndex] = value;
        }
    }

    /// <summary>
    /// 가장 최근에 추가된 데이터 가져오기
    /// </summary>
    public T GetLast()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Buffer is empty.");
        }

        int lastIndex = (_head - 1 + _capacity) % _capacity;
        return _buffer[lastIndex];
    }

    /// <summary>
    /// 가장 오래된 데이터 가져오기
    /// </summary>
    public T GetFirst()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Buffer is empty.");
        }

        int firstIndex = (_head - _count + _capacity) % _capacity;
        return _buffer[firstIndex];
    }

    /// <summary>
    /// 최근 N개 데이터를 Span으로 가져오기 (연속 메모리가 아닐 수 있음)
    /// </summary>
    public void CopyRecentTo(Span<T> destination, int count)
    {
        if (count > _count)
        {
            count = _count;
        }

        if (count > destination.Length)
        {
            throw new ArgumentException("Destination span is too small.", nameof(destination));
        }

        int startIndex = (_head - count + _capacity) % _capacity;

        // 연속된 메모리인 경우
        if (startIndex + count <= _capacity)
        {
            _buffer.AsSpan(startIndex, count).CopyTo(destination);
        }
        else
        {
            // 두 부분으로 나뉘어진 경우
            int firstPart = _capacity - startIndex;
            _buffer.AsSpan(startIndex, firstPart).CopyTo(destination);
            _buffer.AsSpan(0, count - firstPart).CopyTo(destination.Slice(firstPart));
        }
    }

    /// <summary>
    /// 전체 데이터를 배열로 반환 (가장 오래된 데이터부터 정렬됨)
    /// </summary>
    public T[] ToArray()
    {
        var result = new T[_count];
        CopyRecentTo(result, _count);
        return result;
    }

    /// <summary>
    /// 최근 N개 데이터를 배열로 반환
    /// </summary>
    public T[] ToArray(int count)
    {
        if (count > _count)
        {
            count = _count;
        }

        var result = new T[count];
        CopyRecentTo(result, count);
        return result;
    }

    /// <summary>
    /// 내부 버퍼의 Span 가져오기 (메모리 효율적이지만 순서 보장 안됨)
    /// ImPlot 등에서 직접 사용 가능
    /// </summary>
    public Span<T> AsSpan()
    {
        return _buffer.AsSpan(0, _count);
    }

    /// <summary>
    /// 최근 N개의 연속된 메모리 영역 반환 (가능한 경우)
    /// </summary>
    public bool TryGetRecentSpan(int count, out Span<T> span)
    {
        if (count > _count || count <= 0)
        {
            span = Span<T>.Empty;
            return false;
        }

        int startIndex = (_head - count + _capacity) % _capacity;

        // 연속된 메모리인 경우에만 성공
        if (startIndex + count <= _capacity)
        {
            span = _buffer.AsSpan(startIndex, count);
            return true;
        }

        span = Span<T>.Empty;
        return false;
    }

    /// <summary>
    /// 버퍼 초기화
    /// </summary>
    public void Clear()
    {
        _head = 0;
        _count = 0;
        Array.Clear(_buffer, 0, _capacity);
    }

    /// <summary>
    /// 특정 값으로 버퍼 채우기
    /// </summary>
    public void Fill(T value)
    {
        Array.Fill(_buffer, value);
        _head = 0;
        _count = _capacity;
    }

    /// <summary>
    /// 최근 N개 데이터에 대한 열거자
    /// </summary>
    public RecentEnumerator GetRecentEnumerator(int count)
    {
        return new RecentEnumerator(this, count);
    }

    /// <summary>
    /// 최근 데이터 열거를 위한 구조체
    /// </summary>
    public ref struct RecentEnumerator
    {
        private readonly RingBuffer<T> _buffer;
        private readonly int _count;
        private int _currentIndex;

        internal RecentEnumerator(RingBuffer<T> buffer, int count)
        {
            _buffer = buffer;
            _count = Math.Min(count, buffer._count);
            _currentIndex = -1;
        }

        public T Current => _buffer[_currentIndex];

        public bool MoveNext()
        {
            _currentIndex++;
            return _currentIndex < _count;
        }
    }

    /// <summary>
    /// 평균 계산 (숫자 타입에만 사용)
    /// </summary>
    public double Average()
    {
        if (_count == 0)
        {
            return 0;
        }

        double sum = 0;
        for (int i = 0; i < _count; i++)
        {
            sum += Convert.ToDouble(_buffer[i]);
        }

        return sum / _count;
    }

    /// <summary>
    /// 최댓값 찾기 (IComparable 구현 필요)
    /// </summary>
    public T Max()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Buffer is empty.");
        }

        if (!(_buffer[0] is IComparable<T>))
        {
            throw new InvalidOperationException("T must implement IComparable<T>.");
        }

        T max = _buffer[0];
        for (int i = 1; i < _count; i++)
        {
            if (((IComparable<T>)_buffer[i]).CompareTo(max) > 0)
            {
                max = _buffer[i];
            }
        }

        return max;
    }

    /// <summary>
    /// 최솟값 찾기 (IComparable 구현 필요)
    /// </summary>
    public T Min()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Buffer is empty.");
        }

        if (!(_buffer[0] is IComparable<T>))
        {
            throw new InvalidOperationException("T must implement IComparable<T>.");
        }

        T min = _buffer[0];
        for (int i = 1; i < _count; i++)
        {
            if (((IComparable<T>)_buffer[i]).CompareTo(min) < 0)
            {
                min = _buffer[i];
            }
        }

        return min;
    }
}