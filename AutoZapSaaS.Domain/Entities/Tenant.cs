using System;
using System.Collections.Generic;

namespace AutoZapSaaS.Domain.Entities
{
    public class Tenant : Entity
    {
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string Document { get; private set; } // CPF ou CNPJ
        public bool IsActive { get; private set; }

        // Relacionamentos (Entity Framework vai usar isso)
        public virtual ICollection<Instance> Instances { get; private set; }
        public virtual ICollection<Customer> Customers { get; private set; }

        // Construtor: Obriga a informar Nome e Email para criar um Tenant
        public Tenant(string name, string email, string document)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome é obrigatório");
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email é obrigatório");

            Name = name;
            Email = email;
            Document = document;
            IsActive = true; // Nasce ativo
            Instances = new List<Instance>();
            Customers = new List<Customer>();
        }

        // Método de Domínio: Desativar conta (ex: falta de pagamento)
        public void Deactivate()
        {
            IsActive = false;
            SetUpdatedAt();
        }
    }
}