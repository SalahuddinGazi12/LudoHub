using System;

[Serializable]
public class User
{
    public int id;
    public string name;
    public string email;
    public string user_id;
    public string photo;
    public int coins;

    public User() { }

    public User(int id, string name, string email, string user_id)
    {
        this.id = id;
        this.name = name;
        this.email = email;
        this.user_id = user_id;
    }

    public User(User data)
    {
        id = data.id;
        name = data.name;
        email = data.email;
        user_id = data.user_id;
        photo = data.photo;
    }
}