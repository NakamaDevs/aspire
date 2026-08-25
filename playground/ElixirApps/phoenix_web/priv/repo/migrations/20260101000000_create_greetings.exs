defmodule PhoenixWeb.Repo.Migrations.CreateGreetings do
  use Ecto.Migration

  def change do
    create table(:greetings) do
      add :text, :string, null: false

      timestamps(type: :utc_datetime)
    end

    execute(
      "INSERT INTO greetings (text, inserted_at, updated_at) VALUES ('hello from phoenix', NOW(), NOW())",
      "DELETE FROM greetings"
    )
  end
end
