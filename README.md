---

# 🤖 Crypto Agent: The AI-Powered FUD Detector Bot

Crypto Agent is your ultimate ally in the crypto world, safeguarding your Telegram communities against **FUD (Fear, Uncertainty, and Doubt)**. Equipped with state-of-the-art **AI sentiment analysis**, Crypto Agent analyzes messages in real-time to detect FUD, spam, or harmful content and takes proactive actions to maintain a healthy discussion environment.

---

## 🧠 Features

### 💬 **Real-Time Sentiment Analysis**
- Detects FUD, spam, and inappropriate messages in seconds using advanced AI.
- Leverages cutting-edge NLP models for accuracy.

### 🔨 **Automated Moderation**
- Automatically warns, mutes, kicks, or bans users based on the detected sentiment and severity.
- Configurable punishment modes: `AGGRESSIVE`, `MEDIUM`, or `LIGHT`.

### 📊 **Action Logging**
- Maintains a detailed log of flagged messages and actions taken in an SQLite database.
- Provides transparent insights into user behavior.

### 🛠 **Customizable Behavior**
- Adjust moderation thresholds, punishment modes, and more to suit your community's needs.
- Works with both text messages and photo captions.

### 🚀 **Seamless Integration**
- Designed for Telegram groups, with easy setup and high reliability.
- Supports administrators with powerful tools to manage their community.

---

## 📦 Installation

1. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/crypto-agent.git
   cd crypto-agent
   ```

2. **Install Dependencies**
   ```bash
   pip install -r requirements.txt
   ```

3. **Configure Your Bot**
   - Replace `YOUR_BOT_TOKEN` in the `main()` function with your Telegram Bot API token.

4. **Run the Bot**
   ```bash
   python crypto_agent.py
   ```

---

## ⚙️ Configuration

- **Punishment Modes**:
  - `AGGRESSIVE`: Maximum moderation, even minor offenses are punished.
  - `MEDIUM`: Balanced moderation with reasonable thresholds.
  - `LIGHT`: Minimal intervention, ideal for lenient communities.

---



---

## 🧪 Example Workflow

1. A user sends a message in your Telegram group.
2. Crypto Agent analyzes the message's sentiment.
3. If FUD, spam, or harmful content is detected:
   - A punishment is applied based on the configured mode.
   - The action is logged to the SQLite database.
   - A report is sent to the admin channel for transparency.

---

## 🔧 Tech Stack

- **Python**: Core programming language.
- **Telegram Bot API**: Handles communication with Telegram groups.
- **SQLite**: Local database for action logging.

---

## 🖥 Screenshots

### Example Action Report
> **User ID**: `123456789`  
> **Chat ID**: `-1002345281059`  
> **Message**: `"The market is collapsing, sell everything!"`  
> **Sentiment**: `"FUD"` with **95% confidence**  
> **Action**: `Ban`  

---

## 🚧 Roadmap

- [ ] Add support for multiple languages.
- [ ] Integrate advanced spam detection models.
- [ ] Provide analytics dashboards for admins.
- [ ] Allow admins to whitelist specific users.

---

## 🤝 Contributing

Contributions are welcome! Here's how you can help:
1. Fork the repository.
2. Create a new branch (`feature/your-feature`).
3. Commit your changes.
4. Open a pull request.

---

## 📜 License

Crypto Agent is open-source and licensed under the [MIT License](LICENSE).

---

## 🎉 Join the Community

Have questions or ideas? Join our **Telegram Group** for updates, support, and discussions: [Crypto Agent Community](https://t.me/cryptoagentexe).

---

## ❤️ Acknowledgments

- OpenAI for inspiring us to build smarter bots.
- The Telegram Bot API for seamless integration.

---

Empower your crypto community with **Crypto Agent**! 🚀 Let FUD stand no chance.

--- 
