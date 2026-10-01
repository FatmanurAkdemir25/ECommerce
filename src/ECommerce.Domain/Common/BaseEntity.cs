using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Common
{
    public abstract class BaseEntity //bütün entitylerin ortak özelliği
    {
        // Sıralı GUID (UUIDv7): index parçalanmasını azaltır
        public Guid Id { get; set; } = Guid.CreateVersion7();
    }
}
