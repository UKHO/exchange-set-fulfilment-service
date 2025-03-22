using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor
{
    public class TablerDataAnnotationsValidator : IFormValidator
    {
        public Type Component => typeof(DataAnnotationsValidator);

        public Task<bool> ValidateAsync(object validatorInstance, EditContext editContext) => Task.FromResult(editContext.Validate());

        public bool Validate(object validatorInstance, EditContext editContext) => editContext.Validate();
    }
}
