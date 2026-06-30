using AutoMapper;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Domain.Entities;

namespace AutoZapSaaS.Application.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Tenant, TenantResponse>();

        CreateMap<Instance, InstanceResponse>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<Customer, CustomerResponse>();

        CreateMap<MessageTemplate, MessageTemplateResponse>()
            .ForMember(d => d.EventType, o => o.MapFrom(s => s.EventType.ToString()));

        CreateMap<WebhookEvent, WebhookEventResponse>()
            .ForMember(d => d.Platform, o => o.MapFrom(s => s.Platform.ToString()))
            .ForMember(d => d.EventType, o => o.MapFrom(s => s.EventType.ToString()));

        CreateMap<WhatsAppMessage, WhatsAppMessageResponse>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer != null ? s.Customer.Name : ""));
    }
}
