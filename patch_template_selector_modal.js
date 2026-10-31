const fs = require('fs');
const file = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Contracts/TemplateSelectorModal.razor';
let content = fs.readFileSync(file, 'utf8');

const target1 = `    <div class="modal fade show d-block tf-gallery-modal-backdrop" tabindex="-1" role="dialog" aria-modal="true">
        <div class="modal-dialog modal-dialog-centered tf-gallery-modal-dialog">`.replace(/\r?\n/g, '\r\n');

const replacement1 = `    <div class="modal fade show d-block tf-gallery-modal-backdrop" tabindex="-1" role="dialog" aria-modal="true" @onclick="HandleBackdropClick" @onkeydown="HandleKeyDown">
        <div class="modal-dialog modal-dialog-centered tf-gallery-modal-dialog" @onclick:stopPropagation="true">`.replace(/\r?\n/g, '\r\n');

const target2 = `    private async Task CloseAsync()
    {
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
    }`.replace(/\r?\n/g, '\r\n');

const replacement2 = `    private async Task HandleBackdropClick()
    {
        await CloseAsync();
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            await CloseAsync();
        }
    }

    private async Task CloseAsync()
    {
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
    }`.replace(/\r?\n/g, '\r\n');

content = content.replace(target1, replacement1);
content = content.replace(target2, replacement2);

fs.writeFileSync(file, content, 'utf8');
console.log('Successfully updated TemplateSelectorModal.razor with immediate backdrop-close!');
