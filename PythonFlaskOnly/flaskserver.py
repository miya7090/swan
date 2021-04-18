from flask import Flask, request, jsonify, render_template

app = Flask(__name__)

@app.route('/')
def home():
    return "Hey there!"

@app.route('/processImage', methods=['POST'])
def process_image():
    inputParams = request.get_json()
    success = "trueee"
    return success
    
if __name__ == '__main__':
    app.run(debug=True)
