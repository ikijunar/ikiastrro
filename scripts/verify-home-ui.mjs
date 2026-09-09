import fs from 'node:fs/promises';
import assert from 'node:assert/strict';

const tabs = await (await fetch('http://127.0.0.1:9232/json/list')).json();
const socket = new WebSocket(tabs.find(tab => tab.type === 'page').webSocketDebuggerUrl);
await new Promise(resolve => socket.addEventListener('open', resolve, { once: true }));

let sequence = 0;
const pending = new Map();
const errors = [];
socket.addEventListener('message', ({ data }) => {
    const message = JSON.parse(data);
    if (message.method === 'Runtime.exceptionThrown') errors.push(message.params.exceptionDetails.text);
    if (!message.id) return;
    const request = pending.get(message.id);
    pending.delete(message.id);
    if (message.error) request.reject(new Error(JSON.stringify(message.error)));
    else request.resolve(message.result);
});

const send = (method, params = {}) => new Promise((resolve, reject) => {
    const id = ++sequence;
    pending.set(id, { resolve, reject });
    socket.send(JSON.stringify({ id, method, params }));
});
const evaluate = async expression => {
    const result = await send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (result.exceptionDetails) throw new Error(JSON.stringify(result.exceptionDetails));
    return result.result.value;
};
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

try {
    await send('Runtime.enable');
    await send('Page.enable');
    await send('Emulation.setAutoDarkModeOverride', { enabled: false });
    await send('Emulation.setDeviceMetricsOverride', { width: 1672, height: 941, deviceScaleFactor: 1, mobile: false });
    await send('Page.navigate', { url: 'http://localhost:5160/' });
    for (let attempt = 0; attempt < 40; attempt++) {
        if (await evaluate("document.querySelector('h1')?.textContent === 'Discover Your Path'")) break;
        await delay(250);
    }
    await delay(1000);

    const state = await evaluate(`(() => {
        const root = document.querySelector('.landing-page');
        const action = document.querySelector('.generate-chart');
        const before = document.querySelectorAll('.landing-row').length;
        const filter = document.querySelector('#saved-filter');
        filter.value = 'Ramya';
        filter.dispatchEvent(new Event('input', { bubbles: true }));
        return new Promise(resolve => setTimeout(() => resolve({
            font: getComputedStyle(root).fontFamily,
            background: getComputedStyle(root).backgroundColor,
            actionBackground: getComputedStyle(action).backgroundColor,
            actionColor: getComputedStyle(action).color,
            before,
            after: document.querySelectorAll('.landing-row').length,
            scrollWidth: document.documentElement.scrollWidth,
            viewport: innerWidth,
            heading: document.querySelector('h1')?.textContent,
            tagline: document.querySelector('.brand-lockup b')?.textContent
        }), 400));
    })()`);

    console.log(JSON.stringify(state, null, 2));
    assert.match(state.font, /Manrope/);
    assert.equal(state.background, 'rgb(250, 245, 234)');
    assert.equal(state.actionBackground, 'rgb(15, 32, 65)');
    assert.equal(state.actionColor, 'rgb(244, 122, 36)');
    assert.equal(state.heading, 'Discover Your Path');
    assert.equal(state.tagline, 'Where Passion, Purpose & Planets Align.');
    assert.ok(state.after <= state.before);
    assert.ok(state.scrollWidth <= state.viewport);

    await evaluate("document.querySelector('#saved-filter').value=''; document.querySelector('#saved-filter').dispatchEvent(new Event('input',{bubbles:true}))");
    await delay(250);
    const shot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    await fs.writeFile('reports/home-main-screen-implementation.png', Buffer.from(shot.data, 'base64'));
    assert.deepEqual(errors, []);
    console.log(JSON.stringify(state, null, 2));
    console.log('Home UI verification passed.');
} finally {
    socket.close();
}
