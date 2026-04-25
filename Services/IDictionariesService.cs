using APIlog.Server.DTOs.Dictionaries;

namespace APIlog.Server.Services;

public interface IDictionariesService
{
    Task<IEnumerable<DictionaryCategoryDto>> GetCategoriesAsync();
    Task<IEnumerable<DictionaryItemDto>> GetItemsAsync(string entity);
    Task<DictionaryItemDto> CreateItemAsync(string entity, CreateDictionaryItemDto dto);
    Task<DictionaryItemDto> UpdateItemAsync(string entity, int id, UpdateDictionaryItemDto dto);
    Task DeleteItemAsync(string entity, int id);
}
