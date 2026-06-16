using Application.DTOs.Category;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        ApplyCategoryMappings();
    }


    private void ApplyCategoryMappings()
    {
        CreateMap<Category, CategoryResponseDto>();
        CreateMap<CategoryCreateDto, Category>();
        CreateMap<CategoryUpdateDto, Category>();
    }
}