const fs = require('fs');
const readline = require('readline');

async function findLastUserInput() {
    const fileStream = fs.createReadStream('C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901/.system_generated/logs/transcript_full.jsonl');
    const rl = readline.createInterface({
        input: fileStream,
        crlfDelay: Infinity
    });

    let lastUserInput = null;
    for await (const line of rl) {
        if (line.includes('"USER_INPUT"')) {
            try {
                const parsed = JSON.parse(line);
                if (parsed.type === 'USER_INPUT') {
                    lastUserInput = parsed;
                }
            } catch (e) {}
        }
    }

    if (lastUserInput) {
        fs.writeFileSync('C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/last_user_prompt.txt', lastUserInput.content, 'utf8');
        console.log('Saved last user prompt, length:', lastUserInput.content.length);
    } else {
        console.log('No user input found');
    }
}

findLastUserInput();
