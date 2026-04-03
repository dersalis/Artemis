using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Artemis.Shared.Models
{
    public class Measurement
    {
        public Measurement(int actorId, long sequence, long timestamp, int value)
        {
            Id = Guid.NewGuid();
            ActorId = actorId;
            Sequence = sequence;
            Timestamp = timestamp;
            Value = value;
        }

        public Guid Id { get; init; }
        public int ActorId { get; set; }
        public long Sequence { get; set; }
        public long Timestamp { get; set; }
        public int Value { get; init; }
    }
}