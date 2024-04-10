

using System;
using MessagePack;

namespace Effiry
{
    [MessagePackObject()]
    public struct ReactiveValue<T> 
    {
        [Key(0)]
        public T Value
        {
            get => _Value;
            set {

                if (_Value.Equals(value))
                {
                    OnValueChanged.Invoke(value);
                }

                _Value = value;
            }

        }

        private T _Value;
        
        public event Action<T> OnValueChanged;
        
        [SerializationConstructor]
        public ReactiveValue(T value) : this (value, delegate { }) { }
        public ReactiveValue(T value, Action<T> actions)
        {
            _Value = value;
            OnValueChanged = actions;
        }

        public static implicit operator ReactiveValue<T> (T value)
        {
            return new ReactiveValue<T>(value);
        }
        public static explicit operator T (ReactiveValue<T> value)
        {
            return value.Value;
        }
    }
}


    
