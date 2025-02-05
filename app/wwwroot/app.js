document.addEventListener('DOMContentLoaded', () => {
    const resultDiv = document.getElementById('result');
    
    // Test API call
    fetch('/test')
        .then(response => response.text())
        .then(data => {
            resultDiv.textContent = `API Response: ${data}`;
        })
        .catch(error => {
            resultDiv.textContent = `Error: ${error.message}`;
        });
});


function startProcess() {
    fetch('http://localhost:5273/start', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            dir: '/Users/chenhuanqin/Downloads'
        })
    })
    .then(response => {
        const reader = response.body.getReader();
        
        function readStream() {
            reader.read().then(({done, value}) => {
                if (done) {
                    document.getElementById('messages').innerHTML += '<br>Stream complete';
                    return;
                }
                const text = new TextDecoder().decode(value);
                document.getElementById('messages').innerHTML += '<br>' + text;
                readStream();
            });
        }
        
        readStream();
    });
}