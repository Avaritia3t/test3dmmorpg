using System.Collections.Generic;

public interface IDropRulesService
{
    List<DropRule> dropRules { get; }

    Item GenerateEmptyItem(ItemType itemType);
}
