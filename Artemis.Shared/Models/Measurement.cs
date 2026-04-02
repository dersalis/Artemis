using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Artemis.Shared.Models
{
    public class Measurement
    {
        public Measurement(int value)
        {
            Id = Guid.NewGuid();
            Date = DateTime.UtcNow;
            Value = value;
        }

        public Guid Id { get; init; }
        public DateTime Date { get; init; }
        public int Value { get; init; }
    }
}