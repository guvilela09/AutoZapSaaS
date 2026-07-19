using System;

namespace AutoZapSaaS.Domain.Entities
{
    public enum InstanceStatus
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Banned = 99
    }

    public class Instance : Entity, ITenantEntity
    {
        public Guid TenantId { get; private set; }
        public string Name { get; private set; } // Ex: "WhatsApp Suporte"
        public string SessionName { get; private set; } // Nome na Evolution API
        public string Token { get; private set; } // Token de segurança da Evolution
        public InstanceStatus Status { get; private set; }

        // Propriedade de Navegação
        public virtual Tenant Tenant { get; private set; }

        public Instance(Guid tenantId, string name, string sessionName, string token)
        {
            TenantId = tenantId;
            Name = name;
            SessionName = sessionName; // Geralmente único
            Token = token;
            Status = InstanceStatus.Disconnected;
        }

        /// <summary>
        /// Pareamento iniciado, aguardando alguém escanear o QR Code. Ainda NAO esta
        /// conectada: marcar como Connected aqui faria o painel mentir para o tenant.
        /// </summary>
        public void SetConnecting()
        {
            Status = InstanceStatus.Connecting;
            SetUpdatedAt();
        }

        // Métodos para alterar o estado da Instância
        public void SetConnected()
        {
            Status = InstanceStatus.Connected;
            SetUpdatedAt();
        }

        public void SetDisconnected()
        {
            Status = InstanceStatus.Disconnected;
            SetUpdatedAt();
        }
    }
}