using System;
using System.Globalization;

namespace AutoZapSaaS.Domain.Entities
{
    
    public class Customer : Entity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public virtual Tenant? Tenant { get; set; }
        public Customer() { } 

        // O seu construtor completo pode permanecer abaixo sem alterações...

        public Customer(Guid tenantId, string name, string phoneNumber, string email, string origin)
        {
            TenantId = tenantId;
            Name = name;
            // Todo: No futuro, adicionar validação de Regex para telefone aqui
            PhoneNumber = phoneNumber;
            Email = email;
            Origin = origin;
        }

        public void UpdatePhoneNumber(string newNumber)
        {
            PhoneNumber = newNumber;
            SetUpdatedAt();
        }
    }
}