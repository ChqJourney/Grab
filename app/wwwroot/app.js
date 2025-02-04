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
