using AutoMapper;
using GoWithFlow.Application.DTOs.Responses;
using GoWithFlow.Domain.Entities;

namespace GoWithFlow.Application.Mappings;

public sealed class AuthMappingProfile : Profile
{
	public AuthMappingProfile()
	{
		CreateMap<User, UserProfileResponseDto>();
	}
}
