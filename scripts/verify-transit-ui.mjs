import fs from 'node:fs/promises';
import assert from 'node:assert/strict';

const tabs = await (await fetch('http://127.0.0.1:9229/json/list')).json();
const socket = new WebSocket(tabs.find(t => t.type === 'page').webSocketDebuggerUrl);
await new Promise(resolve => socket.addEventListener('open', resolve, { once: true }));
let sequence = 0;
const pending = new Map();
const errors = [];
socket.addEventListener('message', ({ data }) => {
    const message = JSON.parse(data);
    if (message.method === 'Runtime.exceptionThrown') errors.push(message.params.exceptionDetails.text);
    if (message.id) {
        const request = pending.get(message.id);
        pending.delete(message.id);
        if (message.error) request.reject(new Error(JSON.stringify(message.error)));
        else request.resolve(message.result);
    }
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
const waitFor = async expression => {
    for (let n = 0; n < 40; n++) {
        if (await evaluate(expression)) return;
        await delay(250);
    }
    throw new Error('Timed out: ' + expression);
};
try {
    await send('Runtime.enable');
    await send('Page.enable');
    await send('Emulation.setAutoDarkModeOverride', { enabled: false });
    await send('Emulation.setDeviceMetricsOverride', { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false });
    await send('Page.navigate', { url: 'http://127.0.0.1:5099/transit-wheel/1' });
    await waitFor('!!document.querySelector("#active-transit-status")');
    await delay(1500);
    const natal = await evaluate('document.querySelector("#dynamic-natal").innerHTML');
    assert.match(natal, /Su/);
    assert.match(natal, /Gu/);
    const before = await evaluate('document.querySelector("#active-transit-status").textContent');
    await evaluate('document.querySelector("input[type=date]").value = "2027-03-12"; document.querySelector("input[type=date]").dispatchEvent(new Event("change", {bubbles:true}));');
    await waitFor('document.querySelector("#active-transit-status").textContent.includes("12 Mar 2027")');
    assert.equal(await evaluate('document.querySelector("#dynamic-natal").innerHTML'), natal);
    await evaluate('const select = document.querySelectorAll(".selectors select")[0]; select.selectedIndex = 2; select.dispatchEvent(new Event("change", {bubbles:true}));');
    await waitFor('document.querySelector("input[type=date]").value !== "2027-03-12"');
    const maha = await evaluate('document.querySelectorAll(".selectors select")[0].value');
    await evaluate('const antar = document.querySelectorAll(".selectors select")[1]; antar.selectedIndex = 2; antar.dispatchEvent(new Event("change", {bubbles:true}));');
    await delay(750);
    assert.equal(await evaluate('document.querySelectorAll(".selectors select")[0].value'), maha);
    assert.equal(await evaluate('document.querySelector("#dynamic-natal").innerHTML'), natal);
    const desktop = await evaluate('({ width: innerWidth, scroll: document.documentElement.scrollWidth, wheel: (() => { const r = document.querySelector(".wheel-card").getBoundingClientRect(); return {width:r.width,height:r.height,bottom:r.bottom}; })() })');
    console.log(JSON.stringify(desktop));
    assert.ok(desktop.scroll <= desktop.width);
    assert.ok(Math.abs(desktop.wheel.width - desktop.wheel.height) < 1);
    assert.ok(desktop.wheel.bottom <= 900);
    await fs.writeFile('reports/transit-wheel-desktop.jpg', Buffer.from((await send('Page.captureScreenshot', {format:'jpeg',quality:45})).data, 'base64'));
    await send('Page.navigate', { url: 'http://127.0.0.1:5099/transit-wheel/2' });
    await waitFor('!!document.querySelector("#dynamic-natal")');
    assert.notEqual(await evaluate('document.querySelector("#dynamic-natal").innerHTML'), natal);
    await send('Emulation.setDeviceMetricsOverride', { width: 390, height: 844, deviceScaleFactor: 1, mobile: false });
    await delay(500);
    const mobile = await evaluate('({width:innerWidth,scroll:document.documentElement.scrollWidth, table:getComputedStyle(document.querySelector(".comparison")).display, wheel:(() => {const r=document.querySelector(".wheel-card").getBoundingClientRect();return {width:r.width,height:r.height};})()})');
    console.log(JSON.stringify(mobile));
    console.log(await evaluate('Array.from(document.querySelectorAll("body *")).filter(x=>x.getBoundingClientRect().right>390).slice(0,8).map(x=>({tag:x.tagName,cls:x.className,right:x.getBoundingClientRect().right}))'));
    assert.ok(mobile.scroll <= mobile.width);
    assert.ok(Math.abs(mobile.wheel.width - mobile.wheel.height) < 1);
    assert.notEqual(mobile.table, 'none');
    await fs.writeFile('reports/transit-wheel-mobile.jpg', Buffer.from((await send('Page.captureScreenshot', {format:'jpeg',quality:45,captureBeyondViewport:true})).data, 'base64'));
    assert.deepEqual(errors, []);
    console.log(JSON.stringify({result:'passed', desktop, mobile, checks:['date change','maha selection','antar preserves maha','natal fixed across time changes','saved-person binding','square wheel','no horizontal overflow','desktop wheel fits viewport','mobile comparison visible','no JS exceptions']}, null, 2));
} finally {
    socket.close();
}





