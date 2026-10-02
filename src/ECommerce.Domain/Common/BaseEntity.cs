using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Common
{
    //abstract çünkü gerçek bir varlık değil sadece ortak özellik taşıyan temel sınıf
    public abstract class BaseEntity //bütün entitylerin ortak özelliği
    {
        // Sıralı GUID (UUIDv7): index parçalanmasını azaltır
        public Guid Id { get; set; } = Guid.CreateVersion7();
    }
}
