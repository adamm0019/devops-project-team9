class CreateHighScores < ActiveRecord::Migration[7.2]
  def change
    create_table :high_scores do |t|
      t.string :name
      t.string :game
      t.integer :score

      t.timestamps
    end
  end
end