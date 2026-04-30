namespace APIlog.Server.DTOs.Dictionaries;

public record DictionaryItemDto(int Id, string Name, bool CanDelete = false);

public record DictionaryCategoryDto(string Entity, string DisplayName, int Count);

public record CreateDictionaryItemDto(string Name);

public record UpdateDictionaryItemDto(string Name);
