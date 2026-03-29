using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class SavedProperty
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid PropertyId { get; set; }

        public DateTimeOffset SavedAtUtc { get; set; }

        public User User { get; set; } = default!;

        public Property Property { get; set; } = default!;
    }
}
